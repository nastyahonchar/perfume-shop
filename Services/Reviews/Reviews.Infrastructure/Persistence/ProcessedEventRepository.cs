using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Interfaces;

namespace Reviews.Infrastructure.Persistence;

public class ProcessedEventRepository(MongoDbContext context) : IProcessedEventRepository
{
    public const string CollectionName = "processedEvents";

    private readonly IMongoCollection<BsonDocument> _collection =
        context.Database.GetCollection<BsonDocument>(CollectionName);

    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default)
    {
        var count = await _collection.CountDocumentsAsync(
            new BsonDocument("_id", eventId.ToString()),
            new CountOptions { Limit = 1 },
            ct);
        return count > 0;
    }

    public async Task<bool> TryMarkAsync(Guid eventId, string eventType, CancellationToken ct = default)
    {
        try
        {
            await _collection.InsertOneAsync(
                new BsonDocument
                {
                    { "_id", eventId.ToString() },
                    { "eventType", eventType },
                    { "processedAt", DateTime.UtcNow }
                },
                cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}