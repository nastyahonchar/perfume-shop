using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

public class Comment : BaseEntity
{
    public Author Author { get; private set; } = null!;
    public string Text { get; private set; } = "";

    private Comment() { }

    public Comment(Author author, string text)
    {
        if (author is null)
            throw new DomainException("Comment author is required");
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Comment text is required");

        text = text.Trim();
        if (text.Length > 1000)
            throw new DomainException("Comment text is too long (max 1000 characters)");

        Author = author;
        Text = text;
    }
}