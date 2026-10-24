using FluentValidation;

namespace Catalog.Bll.Validators;

public static class ValidationRules
{
    public static IRuleBuilderOptions<T, string?> OneOf<T>(
        this IRuleBuilder<T, string?> rule, IEnumerable<string> allowed, string field)
        => rule.Must(value => allowed.Contains(value?.Trim(), StringComparer.OrdinalIgnoreCase))
               .WithMessage($"{field} must be one of: {string.Join(", ", allowed)}");
}