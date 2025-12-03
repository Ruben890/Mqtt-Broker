using Application.Contract.IRepositories;
using Domain.Entities;
using Infrastruture.Extensions;
using Microsoft.EntityFrameworkCore;
using Persitencia.Contexts;
using Shared.Dtos;
using Shared.Enums;
using Shared.Request;
using Shared.Request.QueryParameters;

namespace Persitencia.Repositories
{
    public class DeviceRepository : RepositoryBase<Device, MqttBrokerContext>, IDeviceRepository
    {
        private readonly MqttBrokerContext _context;
        public DeviceRepository(MqttBrokerContext context) : base(context)
        {
            _context = context;
        }


        public async Task<PagedList<DeviceDto>> GetDevices(DeviceParameters parameters)
        {
            // Query base
            var query = _context.Devices
                .AsNoTracking()
                .AsQueryable();

            // Filtros opcionales

            if (!string.IsNullOrWhiteSpace(parameters.GroupName))
            {
                var groupNameLower = parameters.GroupName.ToLower();
                query = query.Where(d => d.Groups != null &&
                                         d.Groups.GroupName != null &&
                                         d.Groups.GroupName.ToLower().Contains(groupNameLower));
            }

            if (!string.IsNullOrWhiteSpace(parameters.DeviceName))
            {
                var deviceNameLower = parameters.DeviceName.ToLower();
                query = query.Where(d => d.IdentificationName != null &&
                                         d.IdentificationName.ToLower().Contains(deviceNameLower));
            }

            if (!string.IsNullOrWhiteSpace(parameters.FirmwareVersion))
            {
                var firmwareLower = parameters.FirmwareVersion.ToLower();
                query = query.Where(d => d.FirmwareVersion != null &&
                                         d.FirmwareVersion.ToLower().Contains(firmwareLower));
            }

            if (parameters.Status.HasValue)
            {
                query = query.Where(d => d.Status != null && d.Status.Status == parameters.Status.Value);
            }

            // Ordenar: primero los dispositivos con error, luego por CreatedAt descendente
            query = query.OrderByDescending(d =>
                        (d.Status != null && (d.Status.Status == ConnectStatus.Error || !string.IsNullOrEmpty(d.Status.ErrMenssage))) ? 1 : 0)
                     .ThenByDescending(d => d.CreatedAt);

            // Proyección a DTO y paginación
            return await query.Select(x => new DeviceDto
            {
                Id = x.Id,
                ChipId = x.ChipId,
                ChipType = x.ChipType,
                Code = x.Code,
                Description = x.Description,
                ErrMessage = x.Status!.ErrMenssage,
                Status = x.Status.Status.ToString(),
                FirmwareVersion = x.FirmwareVersion,
                GroupDescription = x.Groups!.Description,
                GroupName = x.Groups.GroupName,
                MacAddress = x.MacAddress,
                Name = x.IdentificationName,

            }).ToPaginateAsync(parameters.PageNumber, parameters.PageSize);
        }

        public async Task<Device?> GetDeviceByChipId(string chipId) =>
            await FindByCondition(x => x.ChipId == chipId.ToUpper(), trackChanges: true)
                    .Select(x => new Device
                    {
                        Id = x.Id,
                        GroupId = x.Groups!.Id,
                        ChipId = x.ChipId,
                        ChipType = x.ChipType,
                        Code = x.Code,
                        Description = x.Description,
                        FirmwareVersion = x.FirmwareVersion,
                        MacAddress = x.MacAddress,
                        Status = x.Status,
                        IdentificationName = x.IdentificationName,
                        UpdatedAt = x.UpdatedAt,
                        CreatedAt = x.CreatedAt,
                    }).FirstOrDefaultAsync();

        public async Task<List<string?>> GetDeviceChidIds(GenericParameters parameters)
        {
            // Base query
            IQueryable<Device> query = _context.Devices.AsNoTracking().AsQueryable();

            // Aplicar filtros opcionales
            if (!string.IsNullOrWhiteSpace(parameters.GroupId.ToString()) && parameters.GroupId != Guid.Empty)
                query = query.Where(d => d.GroupId == parameters.GroupId);

            if (!string.IsNullOrWhiteSpace(parameters.DeviceId.ToString()) && parameters.DeviceId != Guid.Empty)
                query = query.Where(d => d.Id == parameters.DeviceId);

            // Ordenar (opcional, para consistencia en streaming)
            query = query.OrderBy(d => d.CreatedAt);

            return await query.Select(x => x.ChipId).ToListAsync();
        }


        public async Task<Device?> GetDeviceById(Guid Id) =>
           await FindByCondition(x => x.Id == Id, trackChanges: false).FirstOrDefaultAsync();

        public async Task CreateDevice(Device device) => await CreateAsyn(device);

        public void UpdateDevice(Device device) => Update(device);
        public async Task CreateDeviceStatus(DeviceStatus deviceStatus) => await _context.DeviceStatuses.AddAsync(deviceStatus);
        public void UpdateDeviceStatus(DeviceStatus deviceStatus) => _context.DeviceStatuses.Update(deviceStatus);
        public void RemoveDevice(Device device) => Delete(device);

    }
}
