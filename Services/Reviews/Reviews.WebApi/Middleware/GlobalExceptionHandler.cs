using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reviews.Domain.Exceptions;

namespace Reviews.WebApi.Middleware;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment env,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            DomainException => (StatusCodes.Status400BadRequest, "Business rule violated"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey }
                => (StatusCodes.Status409Conflict, "Duplicate key"),
            MongoWriteException => (StatusCodes.Status400BadRequest, "Write failed"),
            MongoConnectionException => (StatusCodes.Status503ServiceUnavailable, "Database unavailable"),
            TimeoutException => (StatusCodes.Status503ServiceUnavailable, "Database unavailable"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Request {RequestPath} failed with {Status}", http.Request.Path, status);
        else
            logger.LogWarning("Handled error {Status} on {RequestPath}: {ExceptionType}",
                status, http.Request.Path, ex.GetType().Name);

        ProblemDetails problem = ex is ValidationException validation
            ? new ValidationProblemDetails(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = title;
        problem.Detail = DetailFor(ex, status);
        problem.Instance = http.Request.Path;

        http.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = problem,
            Exception = ex
        });
    }

    private string DetailFor(Exception ex, int status)
    {
        if (ex is ValidationException)
            return "One or more validation errors occurred.";
        if (ex is NotFoundException or DomainException or ConflictException)
            return ex.Message;
        if (env.IsDevelopment())
            return ex.GetBaseException().Message;

        return status switch
        {
            StatusCodes.Status409Conflict => "The operation violates a uniqueness constraint",
            StatusCodes.Status400BadRequest => "The database rejected the write",
            StatusCodes.Status503ServiceUnavailable => "The database is temporarily unavailable",
            _ => "An unexpected error occurred"
        };
    }
}