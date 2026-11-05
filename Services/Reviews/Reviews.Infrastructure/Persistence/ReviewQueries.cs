using AutoMapper;
using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

public class ReviewQueries(MongoDbContext context, IMapper mapper) : IReviewQueries
{
    private readonly IMongoCollection<Review> _reviews = context.Reviews;

    public async Task<ReviewDetailsDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        var review = await _reviews.Find(r => r.Id == objectId).FirstOrDefaultAsync(ct);
        return review is null ? null : mapper.Map<ReviewDetailsDto>(review);
    }

    public async Task<PagedResult<ReviewDto>> GetListAsync(
        long? productId, int page, int pageSize, CancellationToken ct)
    {
        var filter = productId.HasValue
            ? Builders<Review>.Filter.Eq(r => r.ProductId, productId.Value)
            : Builders<Review>.Filter.Empty;

        var total = await _reviews.CountDocumentsAsync(filter, cancellationToken: ct);

        var projection = new BsonDocument
        {
            { "productId", 1 },
            { "productName", 1 },
            { "brandName", 1 },
            { "author.displayName", 1 },
            { "rating", 1 },
            { "title", 1 },
            { "text", 1 },
            { "verifiedPurchase", 1 },
            { "createdAt", 1 },
            { "version", 1 },
            { "commentsCount", new BsonDocument("$size",
                new BsonDocument("$ifNull", new BsonArray { "$comments", new BsonArray() })) }
        };

        var documents = await _reviews.Aggregate()
            .Match(filter)
            .Sort(Builders<Review>.Sort.Descending(r => r.CreatedAt).Descending(r => r.Id))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<BsonDocument>(projection)
            .ToListAsync(ct);

        return new PagedResult<ReviewDto>(
            documents.Select(ToDto).ToList(), (int)total, page, pageSize);
    }

    private static ReviewDto ToDto(BsonDocument d) => new(
        d["_id"].AsObjectId.ToString(),
        d["productId"].ToInt64(),
        d["productName"].AsString,
        OptionalString(d, "brandName"),
        d["author"]["displayName"].AsString,
        d["rating"].ToInt32(),
        OptionalString(d, "title"),
        OptionalString(d, "text"),
        d.TryGetValue("verifiedPurchase", out var verified) && verified.IsBoolean && verified.AsBoolean,
        d["commentsCount"].ToInt32(),
        d["createdAt"].ToUniversalTime(),
        d.TryGetValue("version", out var version) ? version.ToInt32() : 0);

    private static string? OptionalString(BsonDocument document, string name)
        => document.TryGetValue(name, out var value) && value.IsString ? value.AsString : null;
}