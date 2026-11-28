using Catalog.Api.Metrics;
using Platform.ServiceDefaults;
using Catalog.Api.Validation;
using Catalog.Bll.Dtos;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/perfumes")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public class PerfumesController(IPerfumeService perfumes, CatalogMetrics metrics) : ControllerBase
{
    [Authorize(Policy = "products.read")]
    [RequirePermission("products:read")]
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<PerfumeListItemDto>>> GetList(
        [FromQuery] PerfumeQuery query,
        [FromServices] IValidator<PerfumeQuery> validator,
        CancellationToken ct)
    {
        if (await validator.CheckAsync(query, ct) is { } problem)
            return ValidationProblem(problem);

        var result = await metrics.MeasureAsync("list", () => perfumes.GetListAsync(query, ct));

        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Page"] = result.Page.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();

        return Ok(result);
    }

    [Authorize(Policy = "products.read")]
    [RequirePermission("products:read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PerfumeDto>> GetById(int id, CancellationToken ct)
        => Ok(await metrics.MeasureAsync("get", () => perfumes.GetByIdAsync(id, ct)));

    [Authorize(Policy = "products.read")]
    [RequirePermission("products:read")]
    [HttpGet("by-ids")]
    public async Task<ActionResult<IReadOnlyList<PerfumeListItemDto>>> GetByIds(
        [FromQuery] int[] ids, CancellationToken ct)
        => Ok(await metrics.MeasureAsync("get_many", () => perfumes.GetByIdsAsync(ids, ct)));

    [Authorize(Policy = "products.read")]
    [RequirePermission("products:read")]
    [HttpGet("by-note/{noteName}")]
    public async Task<ActionResult<IReadOnlyList<PerfumeListItemDto>>> GetByNote(
        string noteName, [FromQuery] string? layer, CancellationToken ct)
        => Ok(await metrics.MeasureAsync("by_note", () => perfumes.GetByNoteAsync(noteName, layer, ct)));

    [Authorize(Policy = "products.write")]
    [RequirePermission("products:create")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PerfumeDto>> Create(
        CreatePerfumeDto input,
        [FromServices] IValidator<CreatePerfumeDto> validator,
        CancellationToken ct)
    {
        if (await validator.CheckAsync(input, ct) is { } problem)
            return ValidationProblem(problem);

        var created = await metrics.MeasureAsync("create", () => perfumes.CreateAsync(input, ct));
        metrics.ProductCreated(created.Season);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Policy = "products.write")]
    [RequirePermission("products:update")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PerfumeDto>> Update(
        int id,
        UpdatePerfumeDto input,
        [FromServices] IValidator<UpdatePerfumeDto> validator,
        CancellationToken ct)
    {
        if (await validator.CheckAsync(input, ct) is { } problem)
            return ValidationProblem(problem);

        var updated = await metrics.MeasureAsync("update", () => perfumes.UpdateAsync(id, input, ct));
        metrics.ProductUpdated("update");

        return Ok(updated);
    }

    [Authorize(Policy = "products.write")]
    [RequirePermission("products:update")]
    [HttpPut("{id:int}/price")]
    public async Task<ActionResult<PerfumeDto>> ChangePrice(
        int id, ChangePriceDto input, CancellationToken ct)
    {
        var updated = await metrics.MeasureAsync("change_price", () => perfumes.ChangePriceAsync(id, input, ct));
        metrics.ProductUpdated("price_change");

        return Ok(updated);
    }

    [Authorize(Policy = "products.write")]
    [RequirePermission("products:publish")]
    [HttpPost("{id:int}/publish")]
    public async Task<ActionResult<PerfumeDto>> Publish(int id, CancellationToken ct)
    {
        var published = await metrics.MeasureAsync("publish", () => perfumes.PublishAsync(id, ct));
        metrics.ProductUpdated("publish");

        return Ok(published);
    }

    [Authorize(Policy = "products.write")]
    [RequirePermission("products:delete")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await metrics.MeasureAsync("delete", () => perfumes.DeleteAsync(id, ct));
        metrics.ProductDeleted();

        return NoContent();
    }
}