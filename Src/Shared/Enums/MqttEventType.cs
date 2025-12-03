namespace Shared.Enums
{
    /// <summary>
    /// Representa los tipos de eventos MQTT reconocidos por el sistema.
    /// </summary>
    public enum MqttEventType
    {
        Status,
        Telemetry,
        ErrorUpdateFirmwareDevice
    }
}
