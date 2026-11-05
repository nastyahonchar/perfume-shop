using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

public class MongoDbContext
{
    public const string ReviewsCollection = "reviews";

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            throw new InvalidOperationException("MongoDb:ConnectionString is not configured");
        if (string.IsNullOrWhiteSpace(settings.DatabaseName))
            throw new InvalidOperationException("MongoDb:DatabaseName is not configured");

        var client = new MongoClient(settings.ConnectionString);
        Database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<Review> Reviews
        => Database.GetCollection<Review>(ReviewsCollection);
}