using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public sealed class Author : ValueObject
{
    public string UserId { get; }
    public string DisplayName { get; }

    public Author(string userId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId is required");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("DisplayName is required");

        userId = userId.Trim();
        displayName = displayName.Trim();

        if (userId.Length > 100)
            throw new DomainException("UserId is too long (max 100 characters)");
        if (displayName.Length > 100)
            throw new DomainException("DisplayName is too long (max 100 characters)");

        UserId = userId;
        DisplayName = displayName;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return DisplayName;
    }
}