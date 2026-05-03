using Hangfire.Dashboard;
using InventoryManagement.Application.Auth;
using Microsoft.AspNetCore.Hosting;

namespace InventoryManagement.Api.Hangfire;

public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var env = http.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
            return true;
        return http.User?.IsInRole(AppRoles.Admin) == true;
    }
}
