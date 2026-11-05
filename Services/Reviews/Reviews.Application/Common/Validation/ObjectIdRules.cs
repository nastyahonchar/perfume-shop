using FluentValidation;
using MongoDB.Bson;

namespace Reviews.Application.Common.Validation;

public static class ObjectIdRules
{
    public static IRuleBuilderOptions<T, string> MustBeObjectId<T>(this IRuleBuilder<T, string> rule)
        => rule.Must(id => !string.IsNullOrWhiteSpace(id) && ObjectId.TryParse(id, out _))
               .WithMessage("'{PropertyName}' must be a valid ObjectId");
}