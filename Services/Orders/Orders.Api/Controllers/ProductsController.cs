using Microsoft.AspNetCore.Mvc;
using Orders.Bll.Dtos;
using Orders.Bll.Interfaces;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public class ProductsController(IProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(
        [FromQuery] string? season, CancellationToken ct)
        => Ok(string.IsNullOrWhiteSpace(season)
            ? await products.GetAllAsync(ct)
            : await products.GetBySeasonAsync(season, ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductDto>> GetById(long id, CancellationToken ct)
        => Ok(await products.GetByIdAsync(id, ct));

    [HttpPut("{id:long}/stock")]
    public async Task<ActionResult<ProductDto>> SetStock(
        long id, SetStockDto input, CancellationToken ct)
        => Ok(await products.SetStockAsync(id, input.Stock, ct));
}