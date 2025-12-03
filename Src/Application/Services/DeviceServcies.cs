using Application.Contract.IMqtt;
using Application.Contract.IServcies;
using Application.Contract.IUnitOfWork;
using Application.Contracts;
using AutoMapper;
using Shared.Request;
using Shared.Request.QueryParameters;
using Shared.Response;
using System.Net;
using static Shared.Response.BaseResponse;

namespace Application.Services
{
    public class DeviceServcies : IDeviceServcies
    {

        private readonly IMqttBrokerUnitOfWorkManager _repository;
        private readonly ILoggerManager<DeviceServcies> _logger;
        private readonly IMapper _mapper;
        private readonly IMqttServerService _mqttServerService;

        public DeviceServcies
            (IMqttBrokerUnitOfWorkManager repository,
            IMapper mapper,
            IMqttServerService mqttServerService,
            ILoggerManager<DeviceServcies> logger
            )
        {
            _mapper = mapper;
            _mqttServerService = mqttServerService;
            _logger = logger;
            _repository = repository;
        }

        public async Task<BaseResponse> GetDevices(DeviceParameters parameters)
        {

            var devices = await _repository.DeviceRepository.GetDevices(parameters);

            if (devices == null || !devices.Any())
            {
                // No devices found
                return HandleCustomResponse(
                    "No devices found.",
                    HttpStatusCode.NotFound
                );
            }

            // Successfully retrieved devices
            return HandleCustomResponse(
                "Devices retrieved successfully.",
                HttpStatusCode.OK,
                devices,
                devices.Pagination
            );
        }

    }
}
