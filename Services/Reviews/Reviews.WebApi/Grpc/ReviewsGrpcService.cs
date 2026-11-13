using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Reviews.Application.Reviews.Dtos;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Application.Reviews.Queries.GetReviewsList;
using Reviews.Domain.Exceptions;
using Reviews.Grpc;

namespace Reviews.WebApi.Grpc;

public class ReviewsGrpcService(ISender sender) : ReviewsService.ReviewsServiceBase
{
    private const int DefaultPerProduct = 10;
    private const int MaxPerProduct = 50;
    private const int MaxProducts = 100;

    public override async Task<ReviewReply> GetReview(GetReviewRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Review id is required"));

        try
        {
            var review = await sender.Send(new GetReviewByIdQuery(request.Id), context.CancellationToken);
            return ToReply(review);
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, Describe(ex)));
        }
    }

    public override async Task<ReviewsReply> GetForProducts(GetForProductsRequest request, ServerCallContext context)
    {
        var productIds = request.ProductIds.Distinct().ToList();

        if (productIds.Count == 0 || productIds.Any(id => id <= 0))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "At least one positive product id is required"));

        if (productIds.Count > MaxProducts)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"At most {MaxProducts} products can be requested at once"));

        var perProduct = request.PerProduct == 0 ? DefaultPerProduct : request.PerProduct;
        if (perProduct is < 1 or > MaxPerProduct)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"per_product must be between 1 and {MaxPerProduct}"));

        try
        {
            var pages = await Task.WhenAll(productIds.Select(id =>
                sender.Send(new GetReviewsListQuery(id, 1, perProduct), context.CancellationToken)));

            var reply = new ReviewsReply();
            reply.Reviews.AddRange(pages.SelectMany(page => page.Items).Select(ToReply));
            return reply;
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, Describe(ex)));
        }
    }

    private static ReviewReply ToReply(ReviewDto r)
    {
        var reply = new ReviewReply
        {
            Id = r.Id,
            ProductId = r.ProductId,
            ProductName = r.ProductName,
            AuthorName = r.AuthorName,
            Rating = r.Rating,
            VerifiedPurchase = r.VerifiedPurchase,
            CommentsCount = r.CommentsCount,
            CreatedAt = ToTimestamp(r.CreatedAt),
            Version = r.Version
        };

        if (r.BrandName is not null) reply.BrandName = r.BrandName;
        if (r.Title is not null) reply.Title = r.Title;
        if (r.Text is not null) reply.Text = r.Text;

        return reply;
    }

    private static ReviewReply ToReply(ReviewDetailsDto r)
    {
        var reply = new ReviewReply
        {
            Id = r.Id,
            ProductId = r.ProductId,
            ProductName = r.ProductName,
            AuthorName = r.Author.DisplayName,
            Rating = r.Rating,
            VerifiedPurchase = r.VerifiedPurchase,
            CommentsCount = r.Comments.Count,
            CreatedAt = ToTimestamp(r.CreatedAt),
            Version = r.Version
        };

        if (r.BrandName is not null) reply.BrandName = r.BrandName;
        if (r.Title is not null) reply.Title = r.Title;
        if (r.Text is not null) reply.Text = r.Text;

        return reply;
    }

    private static Timestamp ToTimestamp(DateTime value) => Timestamp.FromDateTime(value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    });

    private static string Describe(ValidationException ex)
        => string.Join("; ", ex.Errors.Select(e => e.ErrorMessage).Distinct());
}