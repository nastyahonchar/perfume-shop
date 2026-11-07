using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Models;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
public class AggregatorController(OrderAggregationService aggregation) : ControllerBase
{
    [HttpGet("orders/{id:long}/full")]
    [ProducesResponseType(typeof(FullOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FullOrderDto>> GetFullOrder(long id, CancellationToken ct)
    {
        try
        {
            var result = await aggregation.GetFullOrderAsync(id, ct);

            return result is null
                ? Problem(statusCode: StatusCodes.Status404NotFound,
                    title: "Order not found", detail: $"Order {id} not found")
                : Ok(result);
        }
        catch (UpstreamUnavailableException ex)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Service unavailable", detail: ex.Message);
        }
    }
}