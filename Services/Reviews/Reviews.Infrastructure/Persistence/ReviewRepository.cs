using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Infrastructure.Persistence;

public class ReviewRepository(MongoDbContext context) : IReviewRepository
{
    private readonly IMongoCollection<Review> _reviews = context.Reviews;

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _reviews.Find(r => r.Id == id).FirstOrDefaultAsync(ct);

    public async Task<bool> ExistsAsync(long productId, string authorId, CancellationToken ct = default)
    {
        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.ProductId, productId),
            Builders<Review>.Filter.Eq("author.userId", authorId));

        return await _reviews.Find(filter).Limit(1).AnyAsync(ct);
    }

    public Task AddAsync(Review review, CancellationToken ct = default)
        => _reviews.InsertOneAsync(review, cancellationToken: ct);

    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        var expectedVersion = review.Version;
        review.BumpVersion();

        var versionFilter = expectedVersion == 0
            ? Builders<Review>.Filter.Or(
                Builders<Review>.Filter.Eq(r => r.Version, 0),
                Builders<Review>.Filter.Exists("version", false))
            : Builders<Review>.Filter.Eq(r => r.Version, expectedVersion);

        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.Id, review.Id),
            versionFilter);

        var result = await _reviews.ReplaceOneAsync(filter, review, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new ConflictException(
                $"Review {review.Id} was modified or deleted by someone else (expected version {expectedVersion})");
    }

    public async Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default)
    {
        var result = await _reviews.DeleteOneAsync(Builders<Review>.Filter.Eq(r => r.Id, id), ct);
        return result.DeletedCount > 0;
    }
}