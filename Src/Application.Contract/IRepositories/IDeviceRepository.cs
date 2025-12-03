using Domain.Entities;
using Shared.Dtos;
using Shared.Request;
using Shared.Request.QueryParameters;

namespace Application.Contract.IRepositories
{
    public interface IDeviceRepository
    {
        Task CreateDevice(Device device);
        Task CreateDeviceStatus(DeviceStatus deviceStatus);
        Task<Device?> GetDeviceByChipId(string chipId);
        Task<Device?> GetDeviceById(Guid Id);
        Task<List<string?>> GetDeviceChidIds(GenericParameters parameters);
        Task<PagedList<DeviceDto>> GetDevices(DeviceParameters parameters);
        void RemoveDevice(Device device);
        void UpdateDevice(Device device);
        void UpdateDeviceStatus(DeviceStatus deviceStatus);
    }
}
