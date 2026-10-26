using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Commands.AddComment;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Application.Reviews.Commands.DeleteReview;
using Reviews.Application.Reviews.Commands.UpdateReview;
using Reviews.Application.Reviews.Dtos;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Application.Reviews.Queries.GetReviewsList;
using Reviews.WebApi.Contracts;

namespace Reviews.WebApi.Controllers;

[Route("api/reviews")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public class ReviewsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetList(
        [FromQuery] long? productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetReviewsListQuery(productId, page, pageSize), ct));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDetailsDto>> GetById(string id, CancellationToken ct)
    {
        var review = await Mediator.Send(new GetReviewByIdQuery(id), ct);
        SetETag(review.Version);
        return Ok(review);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<string>> Create(CreateReviewCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDetailsDto>> Update(
        string id,
        UpdateReviewRequest body,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        var version = ParseVersion(ifMatch) ?? body.Version;
        if (version is null)
        {
            ModelState.AddModelError("Version", "Pass the document version in the If-Match header or in the 'version' field");
            return ValidationProblem(ModelState);
        }

        var review = await Mediator.Send(
            new UpdateReviewCommand(id, version.Value, body.Rating, body.Title, body.Text, body.ScentProfile), ct);

        SetETag(review.Version);
        return Ok(review);
    }

    [HttpPost("{id}/comments")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDetailsDto>> AddComment(
        string id, AddCommentRequest body, CancellationToken ct)
    {
        var review = await Mediator.Send(
            new AddCommentCommand(id, body.AuthorId, body.AuthorName, body.Text), ct);

        SetETag(review.Version);
        return Ok(review);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteReviewCommand(id), ct);
        return NoContent();
    }

    private void SetETag(int version) => Response.Headers.ETag = $"\"{version}\"";

    private static int? ParseVersion(string? ifMatch)
        => int.TryParse(ifMatch?.Trim().Trim('"'), out var version) ? version : null;
}