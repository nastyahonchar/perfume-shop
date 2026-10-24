using Catalog.Api.Validation;
using Catalog.Bll.Dtos;
using Catalog.Bll.Interfaces;
using Catalog.Bll.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/perfumes")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public class PerfumesController(IPerfumeService perfumes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<PerfumeListItemDto>>> GetList(
        [FromQuery] PerfumeQuery query,
        [FromServices] IValidator<PerfumeQuery> validator,
        CancellationToken ct)
    {
        if (await validator.CheckAsync(query, ct) is { } problem)
            return ValidationProblem(problem);

        var result = await perfumes.GetListAsync(query, ct);

        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Page"] = result.Page.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PerfumeDto>> GetById(int id, CancellationToken ct)
        => Ok(await perfumes.GetByIdAsync(id, ct));

    [HttpGet("by-ids")]
    public async Task<ActionResult<IReadOnlyList<PerfumeListItemDto>>> GetByIds(
        [FromQuery] int[] ids, CancellationToken ct)
        => Ok(await perfumes.GetByIdsAsync(ids, ct));

    [HttpGet("by-note/{noteName}")]
    public async Task<ActionResult<IReadOnlyList<PerfumeListItemDto>>> GetByNote(
        string noteName, [FromQuery] string? layer, CancellationToken ct)
        => Ok(await perfumes.GetByNoteAsync(noteName, layer, ct));

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

        var created = await perfumes.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

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

        return Ok(await perfumes.UpdateAsync(id, input, ct));
    }

    [HttpPut("{id:int}/price")]
    public async Task<ActionResult<PerfumeDto>> ChangePrice(
        int id, ChangePriceDto input, CancellationToken ct)
        => Ok(await perfumes.ChangePriceAsync(id, input, ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await perfumes.DeleteAsync(id, ct);
        return NoContent();
    }
}