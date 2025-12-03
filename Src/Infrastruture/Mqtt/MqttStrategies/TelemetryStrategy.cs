using Application.Contract.IUnitOfWork;
using Application.Contracts;
using AutoMapper;
using Infrastructure.Interfaces;
using Shared.Enums;

namespace Infrastructure.Mqtt.MqttStrategies
{
    public class TelemetryStrategy : IMqttStrategy
    {
        private readonly ILoggerManager<TelemetryStrategy> _logger;
        private readonly IMqttBrokerUnitOfWorkManager _repository;
        private readonly IMapper _mapper;

        public TelemetryStrategy(
            ILoggerManager<TelemetryStrategy> logger,
            IMqttBrokerUnitOfWorkManager repository,
            IMapper mapper)
        {
            _logger = logger;
            _repository = repository;
            _mapper = mapper;
        }

        public MqttEventType EventType => throw new NotImplementedException();

        public Task Execute(MqttEventType eventType, string topic, string payload)
        {
            throw new NotImplementedException();
        }
    }
}
