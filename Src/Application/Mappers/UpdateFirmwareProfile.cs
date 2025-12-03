using Domain.Entities;
using Shared.Dtos;

namespace Application.Mappers
{
    public class UpdateFirmwareProfile : MappingProfile
    {
        public UpdateFirmwareProfile()
        {
            CreateMap<UpdateFirmwareDto, FirmwareVersionRecord>()
                .ForMember(des => des.Dst, ect => ect.MapFrom(src => src.Dst))
                .ForMember(des => des.ActualVersion, ect => ect.MapFrom(src => src.ActualVersion))
                .ForMember(des => des.CreatedAt, ect => ect.MapFrom(src => src.CreatedAt))
                .ForMember(des => des.Feature, ect => ect.MapFrom(src => src.Feature))
                .ForMember(des => des.UpdatedFromIp, ect => ect.MapFrom(src => src.UpdatedFromIp))
                .ForMember(des => des.FirmwareVersion, ect => ect.MapFrom(src => src.FirmwareVersion))
                .ReverseMap();
        }
    }
}
