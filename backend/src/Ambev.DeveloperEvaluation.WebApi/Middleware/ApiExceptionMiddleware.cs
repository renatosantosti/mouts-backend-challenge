using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

public sealed class ApiExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;

    public ApiExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
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

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, response) = ex switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                CreateError(
                    type: "ValidationError",
                    error: "Invalid input data",
                    detail: string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage))
                )
            ),
            DomainException domainException => (
                StatusCodes.Status400BadRequest,
                CreateError(
                    type: "DomainError",
                    error: "Business rule violation",
                    detail: domainException.Message
                )
            ),
            KeyNotFoundException keyNotFoundException => (
                StatusCodes.Status404NotFound,
                CreateError(
                    type: "ResourceNotFound",
                    error: "Resource not found",
                    detail: keyNotFoundException.Message
                )
            ),
            UnauthorizedAccessException unauthorizedException => (
                StatusCodes.Status401Unauthorized,
                CreateError(
                    type: "AuthenticationError",
                    error: "Unauthorized",
                    detail: unauthorizedException.Message
                )
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                CreateError(
                    type: "ServerError",
                    error: "Unexpected server error",
                    detail: ex.Message
                )
            )
        };

        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static ApiErrorResponse CreateError(string type, string error, string detail) =>
        new()
        {
            Type = type,
            Error = error,
            Detail = detail
        };
}
