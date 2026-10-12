using Microsoft.AspNetCore.Mvc;
using Orders.Bll.Dtos;
using Orders.Bll.Interfaces;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpGet("orders/{id:long}")]
    public async Task<ActionResult<OrderDto>> GetById(long id, CancellationToken ct)
        => Ok(await orders.GetByIdAsync(id, ct));

    [HttpGet("customers/{customerId:long}/orders")]
    public async Task<ActionResult<PagedResultDto<OrderSummaryDto>>> GetByCustomer(
        long customerId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await orders.GetByCustomerAsync(customerId, status, page, pageSize, ct));

    [HttpPost("orders")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderDto input, CancellationToken ct)
    {
        var created = await orders.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("orders/{id:long}")]
    public async Task<ActionResult<OrderDto>> UpdateDetails(
        long id, UpdateOrderDto input, CancellationToken ct)
        => Ok(await orders.UpdateDetailsAsync(id, input, ct));

    [HttpPost("orders/{id:long}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        await orders.ConfirmOrderAsync(id, ct);
        return NoContent();
    }

    [HttpPut("orders/{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeStatus(
        long id, ChangeStatusDto input, CancellationToken ct)
    {
        await orders.ChangeStatusAsync(id, input.Status, ct);
        return NoContent();
    }

    [HttpDelete("orders/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await orders.DeleteAsync(id, ct);
        return NoContent();
    }
}