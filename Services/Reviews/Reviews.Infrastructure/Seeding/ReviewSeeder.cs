using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

public class ReviewSeeder(MongoDbContext context, ILogger<ReviewSeeder> logger) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await context.Reviews.EstimatedDocumentCountAsync(cancellationToken: ct) > 0)
        {
            logger.LogInformation("Reviews collection is not empty, seeding skipped");
            return;
        }

        var reviews = BuildReviews();

        await context.Reviews.InsertManyAsync(reviews, cancellationToken: ct);
        logger.LogInformation("Seeded {Count} reviews", reviews.Count);
    }

    private static List<Review> BuildReviews()
    {
        var full = new Review(
            1, "Pumpkin Velours", "Maison Aurelle", new Author("u-1", "Олена"), 5,
            "Осінь у флаконі", "Тепле гарбузове серце і кориця, тримається цілий день.",
            verifiedPurchase: true,
            scentProfile: new ScentProfile(9, "moderate", new[] { "Autumn", "Winter" }, new[]
            {
                new NoteRating("Pumpkin", 5), new NoteRating("Vanilla", 4), new NoteRating("Cinnamon", 5)
            }),
            photos: new[] { "rev1-a.jpg", "rev1-b.jpg" });
        full.AddComment(new Author("u-7", "Андрій"), "Згоден, мій улюблений на жовтень");
        full.AddComment(new Author("u-3", "Ірина"), "А на літо не занадто солодкий?");
        full.LinkDiscussion(ObjectId.Parse("66f0c3a10000000000000001"));

        var quick = new Review(
            1, "Pumpkin Velours", "Maison Aurelle", new Author("u-5", "Марко"), 4, null, null);

        var withPhoto = new Review(
            5, "Spiced Orchard", "Velvet Atelier", new Author("u-2", "Марина"), 5,
            null, "Яблуко з корицею, як пиріг на кухні в неділю.",
            verifiedPurchase: true, photos: new[] { "rev3-a.jpg" });

        var discussed = new Review(
            5, "Spiced Orchard", "Velvet Atelier", new Author("u-9", "Софія"), 3,
            null, "Після двох тижнів носіння не впевнена, що це мій аромат.");
        discussed.AddComment(new Author("u-2", "Марина"), "Дай йому годину, він розкривається");
        discussed.LinkDiscussion(ObjectId.Parse("66f0c3a10000000000000002"));

        var summer = new Review(
            2, "Citrus Riviera", "Maison Aurelle", new Author("u-4", "Дмитро"), 4,
            null, "Свіжий, але вечором уже майже не чути.",
            scentProfile: new ScentProfile(3, "intimate", new[] { "Summer" }, new[]
            {
                new NoteRating("Bergamot", 5), new NoteRating("Mint", 3)
            }));

        var allYear = new Review(
            6, "Black Iris", "Velvet Atelier", new Author("u-1", "Олена"), 5,
            null, "Ірис і уд, розкішно.",
            verifiedPurchase: true,
            scentProfile: new ScentProfile(12, "strong", new[] { "Autumn", "Winter", "Spring" }, null));

        var critical = new Review(
            7, "Amber Harvest", "Casa Ambra", new Author("u-8", "Тарас"), 2,
            null, "Занадто кавовий, очікував більше бурштину.");

        var quick2 = new Review(
            7, "Amber Harvest", "Casa Ambra", new Author("u-6", "Каріна"), 5, null, null);

        return new List<Review> { full, quick, withPhoto, discussed, summer, allYear, critical, quick2 };
    }
}