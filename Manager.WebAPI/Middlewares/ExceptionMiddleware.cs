using System.Net;
using System.Text.Json;
using Manager.BusinessLogic.Exceptions;
using Manager.WebAPI.Extensions;
using Microsoft.VisualBasic;

namespace Manager.WebAPI.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var(statusCode , message) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, exception.Message),
            ConflictException => (HttpStatusCode.Conflict, exception.Message),
            ValidationAppException => (HttpStatusCode.BadRequest, exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            AuthenticationFailedException => (HttpStatusCode.Unauthorized, exception.Message),
            ForbiddenException => (HttpStatusCode.Forbidden, exception.Message),
            _=> (HttpStatusCode.InternalServerError, "Произошла непредвиденная ошибка. Повторите попытку позже.")
        };

        if(statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception , "Unhanled exception occured.");
        }
        else
        {
            _logger.LogWarning(exception , "Handled exception: {Message}", exception.Message);
        }
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message);
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}