using Catalog.Bll.Dtos;
using Catalog.Bll.Queries;
using Catalog.Domain.Constants;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class SaveBrandDtoValidator : AbstractValidator<SaveBrandDto>
{
    public SaveBrandDtoValidator()
    {
        RuleFor(x => x.FoundedYear)
            .Must(y => y is null || (y >= 1700 && y <= 2100))
            .WithMessage("Founded year must be between 1700 and 2100");
    }
}

public class SaveNoteDtoValidator : AbstractValidator<SaveNoteDto>
{
    public SaveNoteDtoValidator()
    {
        RuleFor(x => x.Family).OneOf(NoteFamilies.All, "Family");
    }
}

public class PerfumeQueryValidator : AbstractValidator<PerfumeQuery>
{
    private static readonly string[] SortFields = { "name", "price" };
    private static readonly string[] SortDirections = { "asc", "desc" };

    public PerfumeQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.SortBy).OneOf(SortFields, "SortBy");
        RuleFor(x => x.SortDir).OneOf(SortDirections, "SortDir");

        RuleFor(x => x.Season).OneOf(Seasons.All, "Season").When(x => !string.IsNullOrWhiteSpace(x.Season));
        RuleFor(x => x.Gender).OneOf(Genders.All, "Gender").When(x => !string.IsNullOrWhiteSpace(x.Gender));

        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x)
            .Must(x => x.MinPrice is null || x.MaxPrice is null || x.MinPrice <= x.MaxPrice)
            .WithMessage("MinPrice must not be greater than MaxPrice");
    }
}