using FluentValidation;
using ProductManager.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace ProductManager.WebAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message, (object?)null),
            ValidationException validation => (
                HttpStatusCode.BadRequest,
                "One or more validation errors occurred.",
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            InvalidOperationException invalidOperation => (HttpStatusCode.BadRequest, invalidOperation.Message, (object?)null),
            ArgumentException argument => (HttpStatusCode.BadRequest, argument.Message, (object?)null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (object?)null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = errors is null
            ? new { message }
            : (object)new { message, errors };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
