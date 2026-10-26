using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Reviews.Domain.Common;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Persistence;

public static class MongoMappings
{
    private static readonly object Sync = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Sync)
        {
            if (_registered)
                return;

            ConventionRegistry.Register(
                "reviews-conventions",
                new ConventionPack
                {
                    new CamelCaseElementNameConvention(),
                    new IgnoreIfNullConvention(true),
                    new IgnoreExtraElementsConvention(true)
                },
                type => type.Namespace is not null
                        && type.Namespace.StartsWith("Reviews.Domain", StringComparison.Ordinal));

            BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
            {
                cm.MapIdProperty(e => e.Id);
                cm.MapProperty(e => e.CreatedAt).SetElementName("createdAt");
                cm.MapProperty(e => e.UpdatedAt).SetElementName("updatedAt").SetIgnoreIfNull(true);
                cm.MapProperty(e => e.Version).SetElementName("version");
            });

            BsonClassMap.RegisterClassMap<Author>(cm =>
            {
                cm.MapProperty(a => a.UserId).SetElementName("userId");
                cm.MapProperty(a => a.DisplayName).SetElementName("displayName");
                cm.MapCreator(a => new Author(a.UserId, a.DisplayName));
                cm.SetIgnoreExtraElements(true);
            });

            BsonClassMap.RegisterClassMap<NoteRating>(cm =>
            {
                cm.MapProperty(n => n.Note).SetElementName("note");
                cm.MapProperty(n => n.Score).SetElementName("score");
                cm.MapCreator(n => new NoteRating(n.Note, n.Score));
                cm.SetIgnoreExtraElements(true);
            });

            BsonClassMap.RegisterClassMap<ScentProfile>(cm =>
            {
                cm.MapProperty(s => s.LongevityHours).SetElementName("longevityHours").SetIgnoreIfNull(true);
                cm.MapProperty(s => s.Sillage).SetElementName("sillage").SetIgnoreIfNull(true);
                cm.MapProperty(s => s.SeasonsOfWear).SetElementName("seasonOfWear");
                cm.MapProperty(s => s.NotesRated).SetElementName("notesRated");
                cm.MapCreator(s => new ScentProfile(s.LongevityHours, s.Sillage, s.SeasonsOfWear, s.NotesRated));
                cm.SetIgnoreExtraElements(true);
            });

            BsonClassMap.RegisterClassMap<Comment>(cm =>
            {
                cm.MapProperty(c => c.Author).SetElementName("author");
                cm.MapProperty(c => c.Text).SetElementName("text");
                cm.SetIgnoreExtraElements(true);
            });

            BsonClassMap.RegisterClassMap<Review>(cm =>
            {
                cm.MapProperty(r => r.ProductId).SetElementName("productId");
                cm.MapProperty(r => r.ProductName).SetElementName("productName");
                cm.MapProperty(r => r.BrandName).SetElementName("brandName").SetIgnoreIfNull(true);
                cm.MapProperty(r => r.Author).SetElementName("author");
                cm.MapProperty(r => r.Rating).SetElementName("rating");
                cm.MapProperty(r => r.Title).SetElementName("title").SetIgnoreIfNull(true);
                cm.MapProperty(r => r.Text).SetElementName("text").SetIgnoreIfNull(true);
                cm.MapProperty(r => r.VerifiedPurchase).SetElementName("verifiedPurchase");
                cm.MapProperty(r => r.ScentProfile).SetElementName("scentProfile").SetIgnoreIfNull(true);
                cm.MapProperty(r => r.DiscussionId).SetElementName("discussionId").SetIgnoreIfNull(true);

                cm.MapField("_photos").SetElementName("photos");
                cm.MapField("_comments").SetElementName("comments");

                cm.MapExtraElementsField("_extraElements");
            });

            _registered = true;
        }
    }
}