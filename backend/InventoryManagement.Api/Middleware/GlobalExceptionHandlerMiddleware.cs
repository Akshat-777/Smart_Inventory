using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryManagement.Application.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace InventoryManagement.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var (status, title, detail) = ex switch
        {
            BusinessRuleException b => (HttpStatusCode.BadRequest, "Business rule violation", b.Message),
            ConcurrencyConflictException c => (HttpStatusCode.Conflict, "Concurrency conflict", c.Message),
            _ => (
                HttpStatusCode.InternalServerError,
                "An error occurred",
                _env.IsDevelopment() ? FormatDevException(ex) : "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;

        var body = new
        {
            type = "https://httpstatuses.com/" + (int)status,
            title,
            detail,
            status = (int)status
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, jsonOptions));
    }

    private static string FormatDevException(Exception ex)
    {
        var parts = new List<string> { ex.Message };
        for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
            parts.Add(inner.Message);
        return string.Join(" => ", parts);
    }
}
