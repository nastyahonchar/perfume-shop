using MongoDB.Bson;
using Reviews.Application.Common.Behaviors;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Reviews.Tests;

public class ValidationTests
{
    private sealed class FakeReviewRepository(bool reviewExists = false) : IReviewRepository
    {
        public Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default) => Task.FromResult<Review?>(null);
        public Task<bool> ExistsAsync(long productId, string authorId, CancellationToken ct = default) => Task.FromResult(reviewExists);
        public Task AddAsync(Review review, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Review review, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default) => Task.FromResult(false);
    }

    private static CreateReviewCommand Command(int rating = 5) => new(
        3, "Cherry Meadow", "Nordic Bloom", "u-20", "Тест", rating, "Весна", "Легкий аромат", null, null);

    [Fact]
    public async Task Command_with_rating_10_is_rejected_before_handler_is_called()
    {
        var behavior = new ValidationBehavior<CreateReviewCommand, string>(
            new[] { new CreateReviewCommandValidator(new FakeReviewRepository()) });

        var handlerCalled = false;

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => behavior.Handle(
            Command(rating: 10),
            ct => { handlerCalled = true; return Task.FromResult("id"); },
            CancellationToken.None));

        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task Valid_command_reaches_handler()
    {
        var behavior = new ValidationBehavior<CreateReviewCommand, string>(
            new[] { new CreateReviewCommandValidator(new FakeReviewRepository()) });

        var handlerCalled = false;

        var result = await behavior.Handle(
            Command(),
            ct => { handlerCalled = true; return Task.FromResult("id"); },
            CancellationToken.None);

        Assert.True(handlerCalled);
        Assert.Equal("id", result);
    }

    [Fact]
    public async Task Duplicate_review_by_same_author_is_rejected_by_async_rule()
    {
        var validator = new CreateReviewCommandValidator(new FakeReviewRepository(reviewExists: true));

        var result = await validator.ValidateAsync(Command());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("already reviewed"));
    }

    [Fact]
    public async Task All_errors_are_collected_together()
    {
        var validator = new CreateReviewCommandValidator(new FakeReviewRepository());
        var invalid = new CreateReviewCommand(0, "", null, "", "", 10, null, null, null, null);

        var result = await validator.ValidateAsync(invalid);

        Assert.True(result.Errors.Count >= 4);
    }

    [Theory]
    [InlineData("abc", false)]
    [InlineData("", false)]
    [InlineData("66f0c2a10000000000000001", true)]
    public void GetReviewById_validates_ObjectId(string id, bool valid)
    {
        var result = new GetReviewByIdQueryValidator().Validate(new GetReviewByIdQuery(id));

        Assert.Equal(valid, result.IsValid);
    }
}