using AutoMapper;
using SmileDentistBackend.Models;
using SmileDentistBackend.Models.Dto;

namespace SmileDentistBackend.Profiles
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<RegisterRequestDTO, ApplicationUser>()
                .ForMember(x => x.UserName, opt => opt.MapFrom(x => x.UserName));
        }
    }
}
