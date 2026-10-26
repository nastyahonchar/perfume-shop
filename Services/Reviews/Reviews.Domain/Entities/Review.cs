using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

public class Review : BaseEntity
{
    private const int MaxPhotos = 10;

    private List<string> _photos = new();       
    private List<Comment> _comments = new();      

#pragma warning disable CS0649
    private BsonDocument? _extraElements;
#pragma warning restore CS0649

    public long ProductId { get; private set; }         
    public string ProductName { get; private set; } = ""; 
    public string? BrandName { get; private set; }    
    public Author Author { get; private set; } = null!;  
    public int Rating { get; private set; }
    public string? Title { get; private set; }
    public string? Text { get; private set; }
    public bool VerifiedPurchase { get; private set; }
    public ScentProfile? ScentProfile { get; private set; }
    public ObjectId? DiscussionId { get; private set; }    

    public IReadOnlyCollection<string> Photos => _photos.AsReadOnly();
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    private Review() { }  

    public Review(
        long productId,
        string productName,
        string? brandName,
        Author author,
        int rating,
        string? title,
        string? text,
        bool verifiedPurchase = false,
        ScentProfile? scentProfile = null,
        IEnumerable<string>? photos = null)
    {
        if (productId <= 0)
            throw new DomainException("ProductId must be positive");
        if (author is null)
            throw new DomainException("Author is required");

        ProductId = productId;
        ProductName = Required(productName, 200, "ProductName");
        BrandName = Optional(brandName, 100, "BrandName");
        Author = author;
        VerifiedPurchase = verifiedPurchase;
        ScentProfile = scentProfile;

        SetContent(rating, title, text);

        foreach (var photo in photos ?? Enumerable.Empty<string>())
            AddPhoto(photo);
    }

    public void Update(int rating, string? title, string? text, ScentProfile? scentProfile)
    {
        SetContent(rating, title, text);
        ScentProfile = scentProfile;
        Touch();
    }

    public void AddComment(Author author, string text)
    {
        _comments.Add(new Comment(author, text));
        Touch();
    }

    public void AddPhoto(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Photo url is required");

        url = url.Trim();
        if (url.Length > 500)
            throw new DomainException("Photo url is too long (max 500 characters)");
        if (_photos.Count >= MaxPhotos)
            throw new DomainException($"A review can have at most {MaxPhotos} photos");

        _photos.Add(url);
        Touch();
    }

    public void LinkDiscussion(ObjectId discussionId)
    {
        DiscussionId = discussionId;
        Touch();
    }

    private void SetContent(int rating, string? title, string? text)
    {
        if (rating is < 1 or > 5)
            throw new DomainException("Rating must be between 1 and 5");

        Rating = rating;
        Title = Optional(title, 200, "Title");
        Text = Optional(text, 2000, "Text");
    }

    private static string Required(string value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required");

        value = value.Trim();
        if (value.Length > maxLength)
            throw new DomainException($"{field} is too long (max {maxLength} characters)");
        return value;
    }

    private static string? Optional(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();
        if (value.Length > maxLength)
            throw new DomainException($"{field} is too long (max {maxLength} characters)");
        return value;
    }
}