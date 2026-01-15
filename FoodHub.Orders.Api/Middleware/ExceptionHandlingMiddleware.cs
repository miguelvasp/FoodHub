using FoodHub.Orders.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace FoodHub.Orders.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            var (statusCode, title) = MapException(ex);
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = ex.Message,
                Instance = context.Request.Path
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }

    private static (int StatusCode, string Title) MapException(Exception exception)
    {
        return exception switch
        {
            DomainValidationException => (StatusCodes.Status400BadRequest, "Validation error"),
            BusinessRuleViolationException => (StatusCodes.Status422UnprocessableEntity, "Business rule violation"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Concurrency conflict"),
            MongoWriteException mongoEx when mongoEx.WriteError.Category == ServerErrorCategory.DuplicateKey
                => (StatusCodes.Status409Conflict, "Duplicate key"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };
    }
}
