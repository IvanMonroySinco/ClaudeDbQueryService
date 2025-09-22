using AutoMapper;
using ClaudeDbQueryService.Infrastructure.External.Models;

namespace ClaudeDbQueryService.Core.Application.Configuration;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<QueryQueryRequest, QueryQueryResponse>()
            .ForMember(dest => dest.Success, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}