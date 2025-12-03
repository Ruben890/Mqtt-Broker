using Application.Contract.IRepositories;
using Domain.Entities;
using Infrastruture.Extensions;
using Microsoft.EntityFrameworkCore;
using Persitencia.Contexts;
using Shared.Dtos;
using Shared.Request;

namespace Persitencia.Repositories
{
    public class FirmwareVersionRecordRepository : RepositoryBase<FirmwareVersionRecord, MqttBrokerContext>, IFirmwareVersionRecordRepository
    {
        private readonly MqttBrokerContext _context;
        public FirmwareVersionRecordRepository(MqttBrokerContext context) : base(context)
        {
            _context = context;
        }



        public async Task<FirmwareVersionRecord?> GetFirmwareById(Guid Id) =>
            await FindByCondition(x => x.Id == Id, false).FirstOrDefaultAsync();

        public async Task<FirmwareVersionRecord?> GetFirmwareByVersion(string version) =>
                await FindByCondition(x => x.FirmwareVersion == version, false).FirstOrDefaultAsync();

        public async Task AddFirmwareVersionRecord(FirmwareVersionRecord record) => await CreateAsyn(record);
        public void UpdateFirmwareVersionRecord(FirmwareVersionRecord record) => Update(record);

        public void RemoveFirmwareVersionRecord(FirmwareVersionRecord record) => Delete(record);

        public async Task<PagedList<UpdateFirmwareDto>> GetFirmwareVersionRecords(GenericParameters parameters)
        {
            // Query base
            var query = _context.FirmwareVersionRecords.AsQueryable();

            // Filtrar por fecha
            if (parameters.Date != null)
            {
                var targetDate = parameters.GetParsedDateTime()?.Date;
                query = query.Where(x => x.CreatedAt!.Date == targetDate);
            }

            return await query
                .OrderByDescending(x => x.ActualVersion)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new UpdateFirmwareDto
                {
                    Id = x.Id,
                    UpdatedFromIp = x.UpdatedFromIp,
                    Dst = x.Dst,
                    Feature = x.Feature,
                    FirmwareVersion = x.FirmwareVersion,
                    ActualVersion = x.ActualVersion,
                    CreatedAt = x.CreatedAt,
                    DevicesUsingFirmware = _context.Devices.Count(d => d.FirmwareVersion == x.FirmwareVersion!.ToLower()),
                })
                .ToPaginateAsync(parameters.PageNumber, parameters.PageSize);
        }
    }
}
