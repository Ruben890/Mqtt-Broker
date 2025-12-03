using Domain.Entities;
using Shared.Dtos.TelemetryRecord;

namespace Application.Mappers
{
    public class TelemetryRecordProfile : MappingProfile
    {
        public TelemetryRecordProfile()
        {
            CreateMap<TelemetryRecordMqtt, TelemetryRecord>()
                
                .ReverseMap();


            CreateMap<TelemetryRecordDto, TelemetryRecord>()
               
                
                .ReverseMap();

        }
    }
}
