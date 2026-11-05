using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

public class IndexCreationService(MongoDbContext context, ILogger<IndexCreationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var keys = Builders<Review>.IndexKeys;

        var models = new[]
        {
            new CreateIndexModel<Review>(
                keys.Ascending("productId").Descending("createdAt"),
                new CreateIndexOptions { Name = "ix_reviews_product_created" }),

            new CreateIndexModel<Review>(
                keys.Ascending("productId").Descending("rating"),
                new CreateIndexOptions { Name = "ix_reviews_product_rating" }),

            new CreateIndexModel<Review>(
                keys.Ascending("author.userId"),
                new CreateIndexOptions { Name = "ix_reviews_author" }),

            new CreateIndexModel<Review>(
                keys.Text("text").Text("title"),
                new CreateIndexOptions { Name = "tx_reviews_text", DefaultLanguage = "none" }),

            new CreateIndexModel<Review>(
                keys.Ascending("productId").Ascending("author.userId"),
                new CreateIndexOptions { Name = "ux_reviews_product_author", Unique = true })
        };

        await context.Reviews.Indexes.CreateManyAsync(models, ct);
        logger.LogInformation("MongoDB indexes for 'reviews' are ensured ({Count})", models.Length);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}