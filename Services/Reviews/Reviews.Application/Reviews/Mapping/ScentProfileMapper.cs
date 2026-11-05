using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Mapping;

public static class ScentProfileMapper
{
    public static ScentProfile? ToDomain(this ScentProfileDto? dto)
        => dto is null
            ? null
            : new ScentProfile(
                dto.LongevityHours,
                dto.Sillage,
                dto.SeasonsOfWear,
                dto.NotesRated?.Select(n => new NoteRating(n.Note, n.Score)));
}