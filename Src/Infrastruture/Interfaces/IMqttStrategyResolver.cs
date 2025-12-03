using Shared.Enums;

namespace Infrastructure.Interfaces
{
    public interface IMqttStrategyResolver
    {
        Task ResolveAsync(MqttEventType eventType, string topic, string payload);
    }
}
