using FluentValidation;
using Reviews.Application.Common.Validation;

namespace Reviews.Application.Reviews.Queries.GetReviewById;

public class GetReviewByIdQueryValidator : AbstractValidator<GetReviewByIdQuery>
{
    public GetReviewByIdQueryValidator()
    {
        RuleFor(x => x.Id).MustBeObjectId();
    }
}