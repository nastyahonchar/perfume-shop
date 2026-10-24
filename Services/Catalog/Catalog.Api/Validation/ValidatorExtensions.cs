using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Validation;

public static class ValidatorExtensions
{
    public static async Task<ValidationProblemDetails?> CheckAsync<T>(
        this IValidator<T> validator, T instance, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(instance, ct);
        return result.IsValid ? null : new ValidationProblemDetails(result.ToDictionary());
    }
}