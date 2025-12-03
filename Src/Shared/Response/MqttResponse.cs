namespace Shared.Response
{
    /// <summary>
    /// Mensaje genérico usado para enviar eventos MQTT a clientes IoT.
    /// </summary>
    public class MqttResponse<TDetails> where TDetails : class
    {
        /// <summary>
        /// Tipo de evento a ejecutar (ej: "REBOOT", "UPDATE_FIRMWARE").
        /// </summary>
        public string? EventType { get; set; }

        /// <summary>
        /// Fecha y hora UTC del evento.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Datos adicionales del evento (pueden ser nulos).
        /// </summary>
        public TDetails? Details { get; set; } = null!;
    }
}
