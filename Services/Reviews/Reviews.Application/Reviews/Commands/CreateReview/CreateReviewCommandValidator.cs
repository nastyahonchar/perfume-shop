using FluentValidation;
using Reviews.Application.Reviews.Validators;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator(IReviewRepository reviews)
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BrandName).MaximumLength(100);
        RuleFor(x => x.AuthorId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Title).MaximumLength(200);
        RuleFor(x => x.Text).MaximumLength(2000);

        RuleFor(x => x.Photos)
            .Must(p => p is null || p.Count <= 10)
            .WithMessage("A review can have at most 10 photos");
        RuleForEach(x => x.Photos).NotEmpty().MaximumLength(500);

        RuleFor(x => x.ScentProfile!)
            .SetValidator(new ScentProfileDtoValidator())
            .When(x => x.ScentProfile is not null);

        RuleFor(x => x.AuthorId)
            .MustAsync(async (cmd, authorId, ct) => !await reviews.ExistsAsync(cmd.ProductId, authorId, ct))
            .WithMessage("You have already reviewed this product")
            .When(x => x.ProductId > 0 && !string.IsNullOrWhiteSpace(x.AuthorId));
    }
}