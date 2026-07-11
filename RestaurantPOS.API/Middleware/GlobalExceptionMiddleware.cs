using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RestaurantPOS.Application.Common.Exceptions;

namespace RestaurantPOS.API.Middleware;

/// <summary>Global exception handler middleware that converts exceptions to ProblemDetails responses.</summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation exception: {@Errors}", ex.Errors);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "ValidationError",
                title = "Validation failed",
                status = 400,
                errors = ex.Errors
            });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Not found: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { type = "NotFound", title = ex.Message, status = 404 });
        }
        catch (ForbiddenAccessException ex)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { type = "Forbidden", title = ex.Message, status = 403 });
        }
        catch (BusinessRuleException ex)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { type = ex.Code, title = ex.Message, status = 422 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { type = "InternalServerError", title = "An unexpected error occurred.", status = 500 });
        }
    }
}
