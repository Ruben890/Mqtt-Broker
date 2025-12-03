using AutoMapper;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Application.Mappers
{
    public class MappingProfile : Profile
    {
        protected readonly GeometryFactory geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public MappingProfile()
        {
            // Aquí puedes agregar más configuraciones si es necesario
        }
    }
}
