using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Bll.Interfaces;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Constants;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Bll.Services;

public class NoteService : INoteService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public NoteService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<NoteDto>> GetAllAsync(string? family, CancellationToken ct)
    {
        IReadOnlyList<Note> notes;

        if (string.IsNullOrWhiteSpace(family))
        {
            notes = await _uow.Notes.GetAllAsync(ct);
        }
        else
        {
            var canonical = NoteFamilies.All.FirstOrDefault(f =>
                                string.Equals(f, family.Trim(), StringComparison.OrdinalIgnoreCase))
                            ?? throw new ValidationException(
                                $"Unknown family '{family}'. Allowed: {string.Join(", ", NoteFamilies.All)}");
            notes = await _uow.Notes.GetByFamilyAsync(canonical, ct);
        }

        return notes.OrderBy(n => n.Name).Select(n => _mapper.Map<NoteDto>(n)).ToList();
    }

    public async Task<NoteDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var note = await _uow.Notes.GetByIdAsync(id, ct)
                   ?? throw new EntityNotFoundException($"Note {id} not found");
        return _mapper.Map<NoteDto>(note);
    }

    public async Task<NoteWithPerfumesDto> GetWithPerfumesAsync(int id, CancellationToken ct)
    {
        var note = await _uow.Notes.GetWithPerfumesAsync(id, ct)
                   ?? throw new EntityNotFoundException($"Note {id} not found");
        return _mapper.Map<NoteWithPerfumesDto>(note);
    }

    public async Task<NoteDto> CreateAsync(SaveNoteDto input, CancellationToken ct)
    {
        var name = input.Name.Trim();
        if (await _uow.Notes.ExistsByNameAsync(name, ct))
            throw new ConflictException($"Note '{name}' already exists");

        var family = NoteFamilies.All.First(f => string.Equals(f, input.Family.Trim(), StringComparison.OrdinalIgnoreCase));

        var note = new Note { Name = name, Family = family };
        await _uow.Notes.AddAsync(note, ct);
        await _uow.SaveChangesAsync(ct);
        return _mapper.Map<NoteDto>(note);
    }
}