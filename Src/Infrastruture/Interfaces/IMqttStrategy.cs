using Shared.Enums;

namespace Infrastructure.Interfaces
{
    /// <summary>
    /// Define la estrategia para manejar diferentes tipos de eventos MQTT.
    /// </summary>
    public interface IMqttStrategy
    {
        MqttEventType EventType { get; } // cada estrategia indica qué tipo maneja

        Task Execute(MqttEventType eventType, string topic, string payload);
    }
}