using Application.Contract.IUnitOfWork;
using Application.Contracts;
using AutoMapper;
using Infrastructure.Interfaces;
using Newtonsoft.Json;
using Shared.Dtos.MqttResponse;
using Shared.Enums;
using Shared.Request;

namespace Infrastructure.Mqtt.MqttStrategies
{
    public class ErrorUpdateFirmwareDeviceStrategy : IMqttStrategy
    {
        private readonly ILoggerManager<ErrorUpdateFirmwareDeviceStrategy> _logger;
        private readonly IMqttBrokerUnitOfWorkManager _repository;
        private readonly IMapper _mapper;

        public ErrorUpdateFirmwareDeviceStrategy(
            ILoggerManager<ErrorUpdateFirmwareDeviceStrategy> logger,
            IMqttBrokerUnitOfWorkManager repository,
            IMapper mapper)
        {
            _mapper = mapper;
            _logger = logger;
            _repository = repository;
        }

        public MqttEventType EventType => MqttEventType.ErrorUpdateFirmwareDevice;

        public async Task Execute(MqttEventType eventType, string topic, string payload)
        {
            _logger.LogInfo($"Processing MQTT event '{eventType}' on topic '{topic}'.");

            var mqttRequest = JsonConvert.DeserializeObject<MqttRequest<UpdateFirmwareMqtt>>(payload);

            if (mqttRequest?.Device == null || mqttRequest.Details == null)
            {
                _logger.LogError($"MQTT payload does not contain valid device or firmware details.");
                return;
            }

            var deviceDto = mqttRequest.Device;

            try
            {
                // Check if device exists in DB
                var deviceEntity = await _repository.DeviceRepository.GetDeviceByChipId(deviceDto.ChipId!);

                if (deviceEntity == null)
                {
                    _logger.LogError($"Device {deviceDto.ChipId} not found. Cannot log firmware error.");
                    return;
                }

                // Actualizar el status con el error
                var status = deviceEntity.Status!;
                status.Status = ConnectStatus.Error;
                status.ErrMenssage = mqttRequest.Details.ErrorMessage;
                status.UpdateTimestamp();

                // Resetear flags para permitir reintento
                status.UpdateInProgress = false;
                status.FirmwareUpdateCompleted = false;
                status.LastFirmwareChunkSent = 0; 

                // Guardar cambios en DB
                _repository.DeviceRepository.UpdateDeviceStatus(status);
                await _repository.SaveAsync();


                await _repository.SaveAsync();

              
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message, ex);
                throw;
            }
        }
    }
}
