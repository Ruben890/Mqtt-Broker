
using Application.Contract.IRepositories;

namespace Application.Contract.IUnitOfWork
{
    public interface IMqttBrokerUnitOfWorkManager : IDisposable, IAsyncDisposable
    {
        bool HasActiveTransaction { get; }
        IDeviceRepository DeviceRepository { get; }
        ITelemetryRecordRepository TelemetryRecordRepository { get; }
        IFirmwareVersionRecordRepository FirmwareVersionRecordRepository { get; }
        IDeviceGroupRepository DeviceGroupRepository { get; }

        Task BeginAsync();
        Task BulkSaveChangesAsync();
        Task CommitAsync();
        IMqttBrokerUnitOfWorkManager CreateNewScope();

        Task RollbackAsync();
        Task SaveAsync();
    }
}
