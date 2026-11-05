using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Tests;

public class DomainTests
{
    private static Author Olena => new("u-1", "Олена");

    [Fact]
    public void Review_with_rating_10_throws_DomainException()
        => Assert.Throws<DomainException>(
            () => new Review(1, "Pumpkin Velours", "Maison Aurelle", Olena, 10, null, null));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void Review_with_rating_out_of_range_throws(int rating)
        => Assert.Throws<DomainException>(
            () => new Review(1, "Pumpkin Velours", null, Olena, rating, null, null));

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Review_with_valid_rating_is_created(int rating)
    {
        var review = new Review(1, "Pumpkin Velours", null, Olena, rating, null, null);

        Assert.Equal(rating, review.Rating);
        Assert.Equal(0, review.Version);
        Assert.Null(review.Text);
    }

    [Fact]
    public void Review_requires_positive_product_id()
        => Assert.Throws<DomainException>(
            () => new Review(0, "Pumpkin Velours", null, Olena, 5, null, null));

    [Fact]
    public void AddComment_with_empty_text_throws()
    {
        var review = new Review(1, "Pumpkin Velours", null, Olena, 5, null, null);

        Assert.Throws<DomainException>(() => review.AddComment(Olena, "  "));
    }

    [Fact]
    public void AddComment_adds_comment_and_exposes_it_read_only()
    {
        var review = new Review(1, "Pumpkin Velours", null, Olena, 5, null, null);

        review.AddComment(new Author("u-7", "Андрій"), "Згоден");

        Assert.Single(review.Comments);
        Assert.NotNull(review.UpdatedAt);
    }

    [Fact]
    public void Author_value_objects_are_compared_by_value()
    {
        var a = new Author("u-1", "Олена");
        var b = new Author("u-1", "Олена");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, new Author("u-2", "Олена"));
    }

    [Fact]
    public void Author_requires_display_name()
        => Assert.Throws<DomainException>(() => new Author("u-1", " "));

    [Fact]
    public void ScentProfile_rejects_unknown_sillage()
        => Assert.Throws<DomainException>(() => new ScentProfile(5, "loud", null, null));

    [Fact]
    public void ScentProfile_rejects_duplicate_note_ratings()
        => Assert.Throws<DomainException>(() => new ScentProfile(
            5, "moderate", null, new[] { new NoteRating("Pumpkin", 5), new NoteRating("pumpkin", 4) }));

    [Fact]
    public void BumpVersion_increments_version()
    {
        var review = new Review(1, "Pumpkin Velours", null, Olena, 5, null, null);

        review.BumpVersion();

        Assert.Equal(1, review.Version);
    }
}