using FluentValidation;
using Reviews.Application.Common.Validation;
using Reviews.Application.Reviews.Validators;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.Id).MustBeObjectId();
        RuleFor(x => x.Version).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Title).MaximumLength(200);
        RuleFor(x => x.Text).MaximumLength(2000);

        RuleFor(x => x.ScentProfile!)
            .SetValidator(new ScentProfileDtoValidator())
            .When(x => x.ScentProfile is not null);
    }
}