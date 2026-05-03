using System.Security.Claims;
using System.Text;
using Hangfire;
using Hangfire.SqlServer;
using InventoryManagement.Api.Hangfire;
using InventoryManagement.Api.Middleware;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Abstractions.Repositories;
using InventoryManagement.Application.Auth;
using InventoryManagement.Infrastructure.Auth;
using InventoryManagement.Infrastructure.Jobs;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Infrastructure.Repositories;
using InventoryManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Inventory Management API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var useSqlite = builder.Configuration.GetValue("Database:UseSqlite", false);
var useForwardedHeaders = builder.Configuration.GetValue("ReverseProxy:UseForwardedHeaders", false);

if (useForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownNetworks.Clear();
        o.KnownProxies.Clear();
    });
}

builder.Services.AddDbContext<ApplicationDbContext>(o =>
{
    if (useSqlite)
        o.UseSqlite(conn);
    else
        o.UseSqlServer(conn);
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<AuditWriter>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IInventoryLedgerService, InventoryLedgerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IInventoryQueryService, InventoryQueryService>();
builder.Services.AddScoped<IStockMovementQueryService, StockMovementQueryService>();
builder.Services.AddScoped<IManualAdjustmentService, ManualAdjustmentService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<LowStockBackgroundJob>();

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
          ?? throw new InvalidOperationException("Jwt settings missing");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = signingKey,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole(AppRoles.Admin));
    options.AddPolicy("CanEdit", p => p.RequireRole(AppRoles.Admin, AppRoles.Manager));
});

builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.WithOrigins(
            builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ??
            new[] { "http://localhost:5173", "http://127.0.0.1:5173" })
        .AllowAnyHeader()
        .AllowAnyMethod()));

if (!useSqlite)
{
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(conn));

    builder.Services.AddHangfireServer();
}

var app = builder.Build();

if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

if (!useSqlite)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthFilter() }
    });
}

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    env = app.Environment.EnvironmentName,
    database = useSqlite ? "sqlite" : "sqlserver"
}));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    await db.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(db, logger);
}

if (!useSqlite)
{
    RecurringJob.AddOrUpdate<LowStockBackgroundJob>(
        "inventory-low-stock",
        j => j.RunAsync(CancellationToken.None),
        Cron.Daily());
}

app.Run();
