using Catalog.Api.Validation;
using Catalog.Bll.Dtos;
using Catalog.Bll.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/notes")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public class NotesController(INoteService notes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NoteDto>>> GetAll(
        [FromQuery] string? family, CancellationToken ct)
        => Ok(await notes.GetAllAsync(family, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NoteDto>> GetById(int id, CancellationToken ct)
        => Ok(await notes.GetByIdAsync(id, ct));

    [HttpGet("{id:int}/perfumes")]
    public async Task<ActionResult<NoteWithPerfumesDto>> GetWithPerfumes(int id, CancellationToken ct)
        => Ok(await notes.GetWithPerfumesAsync(id, ct));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NoteDto>> Create(
        SaveNoteDto input,
        [FromServices] IValidator<SaveNoteDto> validator,
        CancellationToken ct)
    {
        if (await validator.CheckAsync(input, ct) is { } problem)
            return ValidationProblem(problem);

        var created = await notes.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}