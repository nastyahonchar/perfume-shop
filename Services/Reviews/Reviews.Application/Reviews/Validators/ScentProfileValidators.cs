using FluentValidation;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Constants;

namespace Reviews.Application.Reviews.Validators;

public class NoteRatingDtoValidator : AbstractValidator<NoteRatingDto>
{
    public NoteRatingDtoValidator()
    {
        RuleFor(x => x.Note).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Score).InclusiveBetween(1, 5);
    }
}

public class ScentProfileDtoValidator : AbstractValidator<ScentProfileDto>
{
    public ScentProfileDtoValidator()
    {
        RuleFor(x => x.LongevityHours)
            .Must(h => h is null || (h >= 0 && h <= 48))
            .WithMessage("LongevityHours must be between 0 and 48");

        RuleFor(x => x.Sillage)
            .Must(s => string.IsNullOrWhiteSpace(s) ||
                       Sillages.All.Contains(s.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Sillage must be one of: {string.Join(", ", Sillages.All)}");

        RuleForEach(x => x.SeasonsOfWear)
            .Must(s => WearSeasons.All.Contains(s?.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Season of wear must be one of: {string.Join(", ", WearSeasons.All)}");

        RuleForEach(x => x.NotesRated).SetValidator(new NoteRatingDtoValidator());

        RuleFor(x => x.NotesRated)
            .Must(notes => notes is null ||
                           notes.Select(n => n.Note?.Trim().ToLowerInvariant()).Distinct().Count() == notes.Count)
            .WithMessage("The same note cannot be rated twice");
    }
}