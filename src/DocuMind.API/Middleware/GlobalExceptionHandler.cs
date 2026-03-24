using System.Net;
using DocuMind.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.API.Middleware;

public class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            await WriteProblemDetails(context, HttpStatusCode.BadRequest, "Validation Failed",
                ex.Errors.Select(e => e.ErrorMessage).ToArray());
        }
        catch (AIProviderException ex)
        {
            _logger.LogError(ex, "AI provider error: {Provider}", ex.Provider);
            await WriteProblemDetails(context, HttpStatusCode.ServiceUnavailable,
                "AI Provider Error", [ex.Message]);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await WriteProblemDetails(context, HttpStatusCode.NotFound, "Not Found", [ex.Message]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemDetails(context, HttpStatusCode.InternalServerError,
                "Internal Server Error", ["An unexpected error occurred."]);
        }
    }

    private static async Task WriteProblemDetails(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string[] errors)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Extensions = { ["errors"] = errors }
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
