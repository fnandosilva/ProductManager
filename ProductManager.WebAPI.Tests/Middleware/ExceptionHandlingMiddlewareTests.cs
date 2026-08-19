using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ProductManager.Application.Common.Exceptions;
using ProductManager.WebAPI.Middleware;

namespace ProductManager.WebAPI.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static (ExceptionHandlingMiddleware Middleware, DefaultHttpContext Context) CreateSut(
        RequestDelegate next)
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = new ExceptionHandlingMiddleware(next, logger.Object);
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        return (middleware, context);
    }

    private static async Task<JsonElement> ReadResponseBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    [Fact]
    public async Task InvokeAsync_WithNoException_ShouldNotModifyResponse()
    {
        var (middleware, context) = CreateSut(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_ShouldReturn404WithMessage()
    {
        var (middleware, context) = CreateSut(_ => throw new NotFoundException("Product with ID 100001 was not found."));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var body = await ReadResponseBodyAsync(context);
        body.GetProperty("message").GetString().Should().Be("Product with ID 100001 was not found.");
    }

    [Fact]
    public async Task InvokeAsync_WithValidationException_ShouldReturn400WithGroupedErrors()
    {
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required."),
            new("Price", "Price must be greater than zero."),
            new("Name", "Name is too long.")
        };

        var (middleware, context) = CreateSut(_ => throw new ValidationException(failures));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = await ReadResponseBodyAsync(context);
        body.GetProperty("message").GetString().Should().Be("One or more validation errors occurred.");

        var errors = body.GetProperty("errors");
        errors.GetProperty("Name").GetArrayLength().Should().Be(2);
        errors.GetProperty("Price").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_WithMessageOnlyValidationException_ShouldReturn400WithFixedMessage()
    {
        var (middleware, context) = CreateSut(_ => throw new ValidationException("Invalid email or password."));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = await ReadResponseBodyAsync(context);
        body.GetProperty("message").GetString().Should().Be("One or more validation errors occurred.");
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidOperationException_ShouldReturn400WithMessage()
    {
        var (middleware, context) = CreateSut(_ => throw new InvalidOperationException("Insufficient stock."));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = await ReadResponseBodyAsync(context);
        body.GetProperty("message").GetString().Should().Be("Insufficient stock.");
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentException_ShouldReturn400WithMessage()
    {
        var (middleware, context) = CreateSut(_ => throw new ArgumentException("Invalid argument.", "param"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = await ReadResponseBodyAsync(context);
        body.GetProperty("message").GetString().Should().Contain("Invalid argument.");
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentOutOfRangeException_ShouldReturn400()
    {
        var (middleware, context) = CreateSut(_ => throw new ArgumentOutOfRangeException("quantity", "Quantity must be greater than zero."));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_WithUnhandledException_ShouldReturn500WithGenericMessage()
    {
        var (middleware, context) = CreateSut(_ => throw new InvalidCastException("Something went very wrong."));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var body = await ReadResponseBodyAsync(context);
        body.GetProperty("message").GetString().Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetJsonContentType()
    {
        var (middleware, context) = CreateSut(_ => throw new NotFoundException("Not found."));

        await middleware.InvokeAsync(context);

        context.Response.ContentType.Should().Be("application/json");
    }
}
