using System.Collections.ObjectModel;
using Reviews.Domain.Common;
using Reviews.Domain.Constants;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public sealed class ScentProfile : ValueObject
{
    public double? LongevityHours { get; }
    public string? Sillage { get; }
    public IReadOnlyList<string> SeasonsOfWear { get; }
    public IReadOnlyList<NoteRating> NotesRated { get; }

    public ScentProfile(
        double? longevityHours,
        string? sillage,
        IEnumerable<string>? seasonsOfWear,
        IEnumerable<NoteRating>? notesRated)
    {
        if (longevityHours is < 0 or > 48)
            throw new DomainException("Longevity must be between 0 and 48 hours");

        string? canonicalSillage = null;
        if (!string.IsNullOrWhiteSpace(sillage))
        {
            canonicalSillage = Sillages.All.FirstOrDefault(s =>
                string.Equals(s, sillage.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new DomainException(
                    $"Unknown sillage '{sillage}'. Allowed: {string.Join(", ", Sillages.All)}");
        }

        var seasons = new List<string>();
        foreach (var season in seasonsOfWear ?? Enumerable.Empty<string>())
        {
            var canonical = WearSeasons.All.FirstOrDefault(s =>
                string.Equals(s, season?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new DomainException(
                    $"Unknown season '{season}'. Allowed: {string.Join(", ", WearSeasons.All)}");

            if (!seasons.Contains(canonical))
                seasons.Add(canonical);
        }

        var notes = (notesRated ?? Enumerable.Empty<NoteRating>()).ToList();
        if (notes.Select(n => n.Note.ToLowerInvariant()).Distinct().Count() != notes.Count)
            throw new DomainException("The same note cannot be rated twice");

        LongevityHours = longevityHours;
        Sillage = canonicalSillage;
        SeasonsOfWear = new ReadOnlyCollection<string>(seasons);
        NotesRated = new ReadOnlyCollection<NoteRating>(notes);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LongevityHours;
        yield return Sillage;
        yield return string.Join("|", SeasonsOfWear);
        foreach (var note in NotesRated)
            yield return note;
    }
}