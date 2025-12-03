using Domain.Entities;
using Shared.Dtos;
using Shared.Request;

namespace Application.Contract.IRepositories
{
    public interface IFirmwareVersionRecordRepository
    {
        Task AddFirmwareVersionRecord(FirmwareVersionRecord record);
        Task<FirmwareVersionRecord?> GetFirmwareById(Guid Id);
        Task<FirmwareVersionRecord?> GetFirmwareByVersion(string version);
        Task<PagedList<UpdateFirmwareDto>> GetFirmwareVersionRecords(GenericParameters parameters);
        void RemoveFirmwareVersionRecord(FirmwareVersionRecord record);
        void UpdateFirmwareVersionRecord(FirmwareVersionRecord record);
    }
}
