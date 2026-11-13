using Grpc.Core;
using Platform.Aggregator.Models;
using Platform.Aggregator.Resilience;
using Reviews.Grpc;
using System.Diagnostics;

namespace Platform.Aggregator.Clients;

public class ReviewsClient(ReviewsService.ReviewsServiceClient grpc, ILogger<ReviewsClient> logger)
{
    private const string ServiceName = "reviews-api";
    private const int ReviewsPerProduct = 10;

    public async Task<IReadOnlyList<ReviewDto>> GetForProductsAsync(
        IReadOnlyCollection<long> productIds, CancellationToken ct)
    {
        if (productIds.Count == 0)
            return [];

        logger.LogInformation("Calling {Service} gRPC GetForProducts for {ProductsCount} products",
            ServiceName, productIds.Count);
        var stopwatch = Stopwatch.StartNew();

        var request = new GetForProductsRequest { PerProduct = ReviewsPerProduct };
        request.ProductIds.AddRange(productIds);

        try
        {
            var reply = await grpc.GetForProductsAsync(
                request, deadline: ResilienceProfiles.Reviews.CallDeadline(), cancellationToken: ct);

            logger.LogInformation("{Service} responded {GrpcStatus} in {Elapsed} ms with {ReviewsCount} reviews",
                ServiceName, StatusCode.OK, stopwatch.ElapsedMilliseconds, reply.Reviews.Count);

            return reply.Reviews.Select(ToDto).ToList();
        }
        catch (RpcException ex)
        {
            logger.LogWarning("{Service} failed with {GrpcStatus} in {Elapsed} ms",
                ServiceName, ex.StatusCode, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static ReviewDto ToDto(ReviewReply r) => new(
        r.Id,
        r.ProductId,
        r.ProductName,
        r.HasBrandName ? r.BrandName : null,
        r.AuthorName,
        r.Rating,
        r.HasTitle ? r.Title : null,
        r.HasText ? r.Text : null,
        r.VerifiedPurchase,
        r.CommentsCount,
        r.CreatedAt.ToDateTime(),
        r.Version);
}