using FluentValidation;

namespace Reviews.Application.Reviews.Queries.GetReviewsList;

public class GetReviewsListQueryValidator : AbstractValidator<GetReviewsListQuery>
{
    public GetReviewsListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.ProductId!.Value)
            .GreaterThan(0)
            .When(x => x.ProductId.HasValue)
            .WithName("ProductId");
    }
}