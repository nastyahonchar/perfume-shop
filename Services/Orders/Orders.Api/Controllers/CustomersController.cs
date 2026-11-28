using Microsoft.AspNetCore.Authorization;
using Platform.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;
using Orders.Bll.Dtos;
using Orders.Bll.Interfaces;

namespace Orders.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class CustomersController(ICustomerService customers) : ControllerBase
{
    [Authorize(Policy = "orders.read")]
    [RequirePermission("orders:read")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken ct)
        => Ok(await customers.GetAllAsync(ct));

    [Authorize(Policy = "orders.read")]
    [RequirePermission("orders:read")]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<CustomerDto>> GetById(long id, CancellationToken ct)
        => Ok(await customers.GetByIdAsync(id, ct));

    [Authorize(Policy = "orders.write")]
    [RequirePermission("orders:create")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerDto>> Create(SaveCustomerDto input, CancellationToken ct)
    {
        var created = await customers.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Policy = "orders.write")]
    [RequirePermission("orders:update")]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<CustomerDto>> Update(
        long id, SaveCustomerDto input, CancellationToken ct)
        => Ok(await customers.UpdateAsync(id, input, ct));

    [Authorize(Policy = "orders.write")]
    [RequirePermission("orders:delete")]
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await customers.DeleteAsync(id, ct);
        return NoContent();
    }
}