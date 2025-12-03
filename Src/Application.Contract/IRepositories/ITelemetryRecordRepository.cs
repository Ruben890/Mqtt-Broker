using Domain.Entities;

namespace Application.Contract.IRepositories
{
    public interface ITelemetryRecordRepository
    {
        Task CreateTelemetryRecordAsync(TelemetryRecord telemetryRecord);
        void DeleteTelemetryRecord(TelemetryRecord telemetryRecord);
        Task<TelemetryRecord?> GetTelemetryRecordByDeviceId(Guid deviceId);
        void UpdateTelemetryRecord(TelemetryRecord telemetryRecord);
    }
}
