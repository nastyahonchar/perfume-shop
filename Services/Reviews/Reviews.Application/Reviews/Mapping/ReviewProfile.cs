using AutoMapper;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Mapping;

public class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        CreateMap<Author, AuthorDto>();
        CreateMap<NoteRating, NoteRatingDto>();
        CreateMap<ScentProfile, ScentProfileDto>();

        CreateMap<Comment, CommentDto>()
            .ForCtorParam(nameof(CommentDto.Id), o => o.MapFrom(s => s.Id.ToString()));

        CreateMap<Review, ReviewDetailsDto>()
            .ForCtorParam(nameof(ReviewDetailsDto.Id), o => o.MapFrom(s => s.Id.ToString()))
            .ForCtorParam(nameof(ReviewDetailsDto.DiscussionId),
                o => o.MapFrom(s => s.DiscussionId.HasValue ? s.DiscussionId.Value.ToString() : null));
    }
}