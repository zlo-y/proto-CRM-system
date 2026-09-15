using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Manager.WebAPI.Extensions;

// 
// Используется для расширения функциональности маршрутизации конечных точек приложения, добавляя поддержку проверки состояния здоровья приложения.
// 
public static class HealthCheckEndpointExtensions
{
    public static IEndpointRouteBuilder MapAppHealthChecks(this IEndpointRouteBuilder app)
    {
        var responseWriter = new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        };

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("self")
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });

            return app;
        }

    private static async Task WriteHealthCheckResponse(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            }),
            
        };

        await context.Response.WriteAsJsonAsync(response);  
    }
}