using Catalog.Bll.Dtos;
using Catalog.Domain.Constants;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class PerfumeNoteInputDtoValidator : AbstractValidator<PerfumeNoteInputDto>
{
    public PerfumeNoteInputDtoValidator()
    {
        RuleFor(x => x.Layer).OneOf(NoteLayers.All, "Layer");
    }
}

public class CreatePerfumeDetailDtoValidator : AbstractValidator<CreatePerfumeDetailDto>
{
    public CreatePerfumeDetailDtoValidator()
    {
        RuleFor(x => x.LaunchYear)
            .Must(y => y is null || (y >= 1800 && y <= 2100))
            .WithMessage("Launch year must be between 1800 and 2100");

        RuleFor(x => x.Longevity)
            .Must(l => l is null || (l >= 1 && l <= 5))
            .WithMessage("Longevity must be between 1 and 5");
    }
}

public class CreatePerfumeDtoValidator : AbstractValidator<CreatePerfumeDto>
{
    public CreatePerfumeDtoValidator()
    {
        RuleFor(x => x.Sku)
            .Matches("^[A-Z0-9-]+$")
            .WithMessage("SKU may contain only uppercase letters, digits and hyphens");

        RuleFor(x => x.Gender).OneOf(Genders.All, "Gender");
        RuleFor(x => x.Concentration).OneOf(Concentrations.All, "Concentration");
        RuleFor(x => x.Season).OneOf(Seasons.All, "Season");

        RuleFor(x => x.Detail!).SetValidator(new CreatePerfumeDetailDtoValidator()).When(x => x.Detail is not null);

        RuleForEach(x => x.Notes).SetValidator(new PerfumeNoteInputDtoValidator());
        RuleFor(x => x.Notes)
            .Must(notes => notes.Select(n => n.NoteId).Distinct().Count() == notes.Count)
            .WithMessage("The same note cannot be added twice");
    }
}

public class UpdatePerfumeDtoValidator : AbstractValidator<UpdatePerfumeDto>
{
    public UpdatePerfumeDtoValidator()
    {
        RuleFor(x => x.Season).OneOf(Seasons.All, "Season");
    }
}