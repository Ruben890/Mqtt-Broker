using Application.Contract.IRepositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persitencia.Contexts;

namespace Persitencia.Repositories
{
    public class TelemetryRecordRepository : RepositoryBase<TelemetryRecord, MqttBrokerContext>, ITelemetryRecordRepository
    {
        private readonly MqttBrokerContext _context;
        public TelemetryRecordRepository(MqttBrokerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<TelemetryRecord?> GetTelemetryRecordByDeviceId(Guid deviceId) =>
          await FindByCondition(x => x.DeviceId == deviceId, trackChanges: false).FirstOrDefaultAsync();

        public async Task CreateTelemetryRecordAsync(TelemetryRecord telemetryRecord) => await CreateAsyn(telemetryRecord);

        public void UpdateTelemetryRecord(TelemetryRecord telemetryRecord) => Update(telemetryRecord);

        public void DeleteTelemetryRecord(TelemetryRecord telemetryRecord) => Delete(telemetryRecord);
    }
}
