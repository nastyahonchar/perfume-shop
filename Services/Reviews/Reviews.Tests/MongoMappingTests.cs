using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Reviews.Tests;

public class MongoMappingTests
{
    public MongoMappingTests() => MongoMappings.Register();

    private static Review FullReview()
    {
        var review = new Review(
            1, "Pumpkin Velours", "Maison Aurelle", new Author("u-1", "Олена"), 5,
            "Осінь у флаконі", "Тепле гарбузове серце",
            verifiedPurchase: true,
            scentProfile: new ScentProfile(9, "moderate", new[] { "Autumn", "Winter" },
                new[] { new NoteRating("Pumpkin", 5) }),
            photos: new[] { "rev1-a.jpg" });
        review.AddComment(new Author("u-7", "Андрій"), "Згоден");
        return review;
    }

    [Fact]
    public void Document_has_camelCase_structure_from_Lab01()
    {
        var doc = FullReview().ToBsonDocument();

        Assert.True(doc.Contains("_id"));
        Assert.Equal(1, doc["productId"].ToInt64());
        Assert.Equal("Pumpkin Velours", doc["productName"].AsString);
        Assert.Equal("u-1", doc["author"]["userId"].AsString);
        Assert.Equal("Олена", doc["author"]["displayName"].AsString);
        Assert.True(doc["verifiedPurchase"].AsBoolean);
        Assert.Equal("rev1-a.jpg", doc["photos"].AsBsonArray[0].AsString);
        Assert.Equal("Андрій", doc["comments"].AsBsonArray[0]["author"]["displayName"].AsString);
        Assert.Equal(9.0, doc["scentProfile"]["longevityHours"].ToDouble());
        Assert.Equal("Autumn", doc["scentProfile"]["seasonOfWear"].AsBsonArray[0].AsString);
        Assert.Equal("Pumpkin", doc["scentProfile"]["notesRated"].AsBsonArray[0]["note"].AsString);
    }

    [Fact]
    public void Document_does_not_leak_internal_dotnet_fields()
    {
        var doc = FullReview().ToBsonDocument();

        Assert.False(doc.Contains("_photos"));
        Assert.False(doc.Contains("_comments"));
        Assert.False(doc.Contains("_extraElements"));
        Assert.False(doc.Contains("Comments"));
        Assert.False(doc.Contains("Photos"));
        Assert.False(doc.Contains("_t"));
    }

    [Fact]
    public void Null_optional_fields_are_not_written()
    {
        var doc = new Review(1, "Pumpkin Velours", null, new Author("u-5", "Марко"), 4, null, null).ToBsonDocument();

        Assert.False(doc.Contains("text"));
        Assert.False(doc.Contains("title"));
        Assert.False(doc.Contains("brandName"));
        Assert.False(doc.Contains("scentProfile"));
        Assert.False(doc.Contains("discussionId"));
        Assert.False(doc.Contains("updatedAt"));
    }

    [Fact]
    public void Review_survives_round_trip_through_BSON()
    {
        var original = FullReview();

        var restored = BsonSerializer.Deserialize<Review>(original.ToBsonDocument());

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.ProductId, restored.ProductId);
        Assert.Equal(original.Author, restored.Author);
        Assert.Equal(original.ScentProfile, restored.ScentProfile);
        Assert.Equal(original.Photos, restored.Photos);
        Assert.Single(restored.Comments);
        Assert.Equal(new Author("u-7", "Андрій"), restored.Comments.Single().Author);
    }

    [Fact]
    public void Lab01_document_with_unknown_fields_is_read_and_unknown_fields_are_preserved()
    {
        var doc = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "productId", 5 },
            { "productName", "Spiced Orchard" },
            { "author", new BsonDocument { { "userId", "u-9" }, { "displayName", "Софія" } } },
            { "rating", 3 },
            { "text", "Після двох тижнів носіння не впевнена" },
            { "wearInfo", new BsonDocument { { "daysTested", 14 }, { "occasion", "office" } } },
            { "discussionId", ObjectId.Parse("66f0c3a10000000000000002") },
            { "createdAt", new BsonDateTime(DateTime.UtcNow) },
            { "comments", new BsonArray
                {
                    new BsonDocument
                    {
                        { "author", new BsonDocument { { "userId", "u-2" }, { "displayName", "Марина" } } },
                        { "text", "Дай йому годину" },
                        { "createdAt", new BsonDateTime(DateTime.UtcNow) }
                    }
                }
            }
        };

        var review = BsonSerializer.Deserialize<Review>(doc);

        Assert.Equal(5, review.ProductId);
        Assert.Equal(0, review.Version);
        Assert.Single(review.Comments);
        Assert.NotNull(review.DiscussionId);

        var written = review.ToBsonDocument();

        Assert.True(written.Contains("wearInfo"));
        Assert.Equal(14, written["wearInfo"]["daysTested"].AsInt32);
    }
}