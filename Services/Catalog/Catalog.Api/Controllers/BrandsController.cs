using Catalog.Api.Validation;
using Platform.ServiceDefaults;
using Catalog.Bll.Dtos;
using Catalog.Bll.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/brands")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public class BrandsController(IBrandService brands) : ControllerBase
{
    [Authorize(Policy = "products.read")]
    [RequirePermission("products:read")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> GetAll(CancellationToken ct)
        => Ok(await brands.GetAllAsync(ct));

    [Authorize(Policy = "products.read")]
    [RequirePermission("products:read")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BrandDto>> GetById(int id, CancellationToken ct)
        => Ok(await brands.GetByIdAsync(id, ct));

    [Authorize(Policy = "products.write")]
    [RequirePermission("products:create")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BrandDto>> Create(
        SaveBrandDto input,
        [FromServices] IValidator<SaveBrandDto> validator,
        CancellationToken ct)
    {
        if (await validator.CheckAsync(input, ct) is { } problem)
            return ValidationProblem(problem);

        var created = await brands.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Policy = "products.write")]
    [RequirePermission("products:update")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BrandDto>> Update(
        int id,
        SaveBrandDto input,
        [FromServices] IValidator<SaveBrandDto> validator,
        CancellationToken ct)
    {
        if (await validator.CheckAsync(input, ct) is { } problem)
            return ValidationProblem(problem);

        return Ok(await brands.UpdateAsync(id, input, ct));
    }
}