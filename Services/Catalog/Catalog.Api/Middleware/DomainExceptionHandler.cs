using Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Middleware;

public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violated"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict"),
            DbUpdateException => (StatusCodes.Status409Conflict, "Data conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Unhandled exception on {RequestPath}", http.Request.Path);
        else
            logger.LogWarning("Handled error {Status} on {RequestPath}: {Message}", status, http.Request.Path, ex.Message);

        var detail = ex switch
        {
            DbUpdateConcurrencyException => "The record was changed by someone else. Reload it and try again",
            DbUpdateException => "The operation violates a data constraint",
            _ when status == StatusCodes.Status500InternalServerError => "An unexpected error occurred",
            _ => ex.Message
        };

        http.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = http.Request.Path
            },
            Exception = ex
        });
    }
}