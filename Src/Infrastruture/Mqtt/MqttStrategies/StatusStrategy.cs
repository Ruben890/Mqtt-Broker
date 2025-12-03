using Application.Contract.IUnitOfWork;
using Application.Contracts;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Interfaces;
using Newtonsoft.Json;
using Shared.Dtos;
using Shared.Enums;
using Shared.Request;
using Shared.Utils;

namespace Infrastructure.Mqtt.MqttStrategies
{
    public class StatusStrategy : IMqttStrategy
    {
        private readonly ILoggerManager<StatusStrategy> _logger;
        private readonly IMqttBrokerUnitOfWorkManager _repository;
        private readonly IMapper _mapper;


        public StatusStrategy(
            ILoggerManager<StatusStrategy> logger,
            IMqttBrokerUnitOfWorkManager repository,
            IMapper mapper)
        {
            _mapper = mapper;
            _logger = logger;
            _repository = repository;
        }

        public MqttEventType EventType => MqttEventType.Status;

        public async Task Execute(MqttEventType eventType, string topic, string payload)
        {
            _logger.LogInfo($"Processing MQTT event '{eventType}' on topic '{topic}' ");

            Guid? userId = null;

            var mqttRequest = JsonConvert.DeserializeObject<MqttRequest<object>>(payload);

            if (mqttRequest?.Device == null)
            {
                _logger.LogError("MQTT payload does not contain valid device information.");
                return;
            }

            var deviceDto = mqttRequest.Device;

            try
            {
                var existingDevice = await _repository.DeviceRepository.GetDeviceByChipId(deviceDto!.ChipId!);
                Device deviceEntity;

                if (existingDevice == null)
                {
                    deviceEntity = _mapper.Map<Device>(deviceDto);

                    deviceEntity.CreatedAt = mqttRequest.Timestamp;
                    deviceEntity.Code = ToolsUtils.GenericCode(10);
                    deviceEntity.GroupId = await GetOrCreateDeviceGroupIdAsync(deviceDto)
                                            ?? throw new NullReferenceException("Failed to resolve or create a valid Device Group ID.");

                    await _repository.DeviceRepository.CreateDevice(deviceEntity);
                    await _repository.SaveAsync();

                    deviceEntity.Status = new DeviceStatus
                    {
                        Status = ConnectStatus.Online,
                        DeviceId = deviceEntity.Id,
                        CreatedAt = mqttRequest.Timestamp
                    };

                    await _repository.DeviceRepository.CreateDeviceStatus(deviceEntity.Status);
                }
                else
                {
                    deviceEntity = existingDevice;

                    UpdateFirmwareVersionIfChanged(existingDevice, deviceDto);

                    deviceEntity.Status!.Status = ConnectStatus.Online;
                    deviceEntity.Status!.UpdateTimestamp();

                    _repository.DeviceRepository.UpdateDeviceStatus(deviceEntity.Status);

                    _logger.LogInfo($"Existing device found: {deviceEntity.ChipId}");
                }

                await _repository.SaveAsync();

                deviceDto.Status = ConnectStatus.Online.ToString();
            }
            catch (Exception ex)
            {
                deviceDto.Status = ConnectStatus.Error.ToString();
                _logger.LogError(ex.InnerException?.Message ?? ex.Message, ex);
            }

            if (userId.HasValue && userId != Guid.Empty)
            {
                _logger.LogInfo($"Sending status update for device {deviceDto.ChipId} to user {userId}");
            }
            else
            {
                _logger.LogWarn($"Device {deviceDto.ChipId} has no associated user.");
            }
        }


        private async Task<Guid?> GetOrCreateDeviceGroupIdAsync(DeviceDto deviceDto)
        {
            if (string.IsNullOrWhiteSpace(deviceDto.GroupName))
                throw new ArgumentNullException(nameof(deviceDto.GroupName), "Group name is required.");

            // Check if the group already exists
            var existingGroup = await _repository.DeviceGroupRepository
                .GetDeviceGroupByName(deviceDto.GroupName!);

            if (existingGroup != null)
                return existingGroup.Id;

            // Create the new group
            var newGroup = new Groups
            {
                GroupName = deviceDto.GroupName!,
                Description = deviceDto.GroupDescription,
                IsUnique = true,
                IsActive = true,
                Code = ToolsUtils.GenericCode(10),
                CreatedAt = DateTime.UtcNow
            };

            await _repository.DeviceGroupRepository.CreateGroupAsync(newGroup);
            await _repository.SaveAsync();

            _logger.LogInfo($"A new device group was created: {deviceDto.GroupName}");

            return newGroup.Id;
        }


        private void UpdateFirmwareVersionIfChanged(Device existingDevice, DeviceDto deviceDto)
        {
            // Normalize versions: remove leading 'v' or 'V' and trim spaces
            var existingVersion = existingDevice.FirmwareVersion?.Trim().TrimStart('v', 'V');
            var newVersion = deviceDto.FirmwareVersion?.Trim().TrimStart('v', 'V');

            // Compare ignoring case and leading 'v'
            if (!string.Equals(existingVersion, newVersion, StringComparison.OrdinalIgnoreCase))
            {
                existingDevice.FirmwareVersion = deviceDto.FirmwareVersion!;
                existingDevice.UpdateTimestamp();

                _logger.LogInfo($"Device {deviceDto.ChipId} firmware version updated to {deviceDto.FirmwareVersion}.");

                _repository.DeviceRepository.UpdateDevice(existingDevice);
            }
        }

    }
}
