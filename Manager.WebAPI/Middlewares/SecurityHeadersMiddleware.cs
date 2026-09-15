namespace Manager.WebAPI.Middlewares;

// 
// Обработчик промежуточного программного обеспечения, который добавляет заголовки безопасности к HTTP-ответам, чтобы улучшить защиту приложения от различных атак, таких как XSS, Clickjacking и утечки данных.
// 
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

        if(!_environment.IsDevelopment())
        {
            context.Response.Headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'");
        }


        await _next(context);
    }
}