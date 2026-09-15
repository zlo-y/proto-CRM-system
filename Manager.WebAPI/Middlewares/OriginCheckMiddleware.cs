namespace Manager.WebAPI.Middlewares;

// 
// Обработчик промежуточного программного обеспечения, который проверяет источник (Origin) входящих HTTP-запросов и блокирует запросы с недопустимыми источниками, если они не являются безопасными методами и содержат JWT в куки.
// 
public class OriginCheckMiddleware
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD" , "OPTIONS"
    };

    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedOrigins;

    public OriginCheckMiddleware(RequestDelegate next , IConfiguration configuration)
    {
        _next = next;
        _allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?.ToHashSet() ??
        new HashSet<string>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if(!SafeMethods.Contains(context.Request.Method) && context.Request.Cookies.ContainsKey("jwt"))
        {
            var origin = context.Request.Headers.Origin.ToString();
            if(string.IsNullOrEmpty(origin) || !_allowedOrigins.Contains(origin))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new {Status = 403, Message = "Недопустимый источник запроса."});
                return;
            }
        }
        await _next(context);
    }
}