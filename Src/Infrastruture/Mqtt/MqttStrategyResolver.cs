using Application.Contracts;
using Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Shared.Enums;

namespace Infrastructure.Mqtt
{
    public sealed class MqttStrategyResolver : IMqttStrategyResolver
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILoggerManager<MqttStrategyResolver> _logger;

        public MqttStrategyResolver(IServiceScopeFactory scopeFactory, ILoggerManager<MqttStrategyResolver> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ResolveAsync(MqttEventType eventType, string topic, string payload)
        {
            using var scope = _scopeFactory.CreateScope();

            var strategies = scope.ServiceProvider.GetServices<IMqttStrategy>();

            foreach (var strategy in strategies)
            {
                if (strategy.EventType == eventType)
                {
                    _logger.LogInfo($"Executing strategy for event: {eventType}");
                    await strategy.Execute(eventType, topic, payload);
                    return;
                }
            }

            _logger.LogWarn($"No strategy found for event: {eventType}");
        }
    }
}
