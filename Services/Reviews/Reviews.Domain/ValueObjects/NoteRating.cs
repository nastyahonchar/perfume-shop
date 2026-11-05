using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public sealed class NoteRating : ValueObject
{
    public string Note { get; }
    public int Score { get; }

    public NoteRating(string note, int score)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new DomainException("Note name is required");

        note = note.Trim();
        if (note.Length > 60)
            throw new DomainException("Note name is too long (max 60 characters)");
        if (score is < 1 or > 5)
            throw new DomainException("Note score must be between 1 and 5");

        Note = note;
        Score = score;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Note;
        yield return Score;
    }
}