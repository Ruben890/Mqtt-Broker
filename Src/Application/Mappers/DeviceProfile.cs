using Domain.Entities;
using Shared.Dtos;

namespace Application.Mappers
{
    public class DeviceProfile : MappingProfile
    {
        public DeviceProfile()
        {
            // DTO -> Domain
            CreateMap<DeviceDto, Device>()
               .ForMember(dest => dest.IdentificationName, opt => opt.MapFrom(src => src.Name))
               .ForMember(dest => dest.MacAddress, opt => opt.MapFrom(src => src.MacAddress ?? string.Empty))
               .ForMember(dest => dest.ChipType, opt => opt.MapFrom(src => src.ChipType ?? string.Empty))
               .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code ?? string.Empty))
               .ForMember(dest => dest.Status, opt => opt.Ignore())
               .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description));

            // Domain -> DTO
            CreateMap<Device, DeviceDto>()
                 .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.IdentificationName))
                 .ForMember(dest => dest.MacAddress, opt => opt.MapFrom(src => src.MacAddress))
                 .ForMember(dest => dest.ChipType, opt => opt.MapFrom(src => src.ChipType))
                 .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code))
                 .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                 .ForMember(dest => dest.FirmwareVersion, opt => opt.MapFrom(src => src.FirmwareVersion));

        }
    }
}
