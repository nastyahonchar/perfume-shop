using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Mapping;

public class CatalogProfile : Profile
{
    public CatalogProfile()
    {
        CreateMap<Brand, BrandDto>();
        CreateMap<Note, NoteDto>();
        CreateMap<PerfumeImage, PerfumeImageDto>();
        CreateMap<PerfumeDetail, PerfumeDetailDto>();

        CreateMap<PerfumeNote, PerfumeNoteDto>()
            .ForCtorParam(nameof(PerfumeNoteDto.NoteName), o => o.MapFrom(s => s.Note.Name))
            .ForCtorParam(nameof(PerfumeNoteDto.Family), o => o.MapFrom(s => s.Note.Family));

        CreateMap<Perfume, PerfumeListItemDto>()
            .ForCtorParam(nameof(PerfumeListItemDto.Brand), o => o.MapFrom(s => s.Brand.Name));

        CreateMap<Perfume, PerfumeDto>()
            .ForCtorParam(nameof(PerfumeDto.Brand), o => o.MapFrom(s => s.Brand.Name))
            .ForCtorParam(nameof(PerfumeDto.Notes), o => o.MapFrom(s => s.PerfumeNotes));

        CreateMap<Note, NoteWithPerfumesDto>()
            .ForCtorParam(nameof(NoteWithPerfumesDto.Perfumes),
                o => o.MapFrom(s => s.PerfumeNotes.Select(pn => pn.Perfume)));
    }
}