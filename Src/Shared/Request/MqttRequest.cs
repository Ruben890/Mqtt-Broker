using Shared.Dtos;

namespace Shared.Request
{
    /// <summary>
    /// Representa un mensaje MQTT que contiene información del dispositivo y datos específicos.
    /// </summary>
    /// <typeparam name="TDetails">Tipo de los detalles específicos del mensaje.</typeparam>
    public class MqttRequest<TDetails> where TDetails : class
    {
        /// <summary>
        /// Información básica del dispositivo que envía el mensaje.
        /// </summary>
        public DeviceDto? Device { get; set; }

        /// <summary>
        /// Marca de tiempo en que se genera el mensaje (UTC).
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Detalles específicos del mensaje, definidos por el tipo genérico TDetails.
        /// Puede ser nulo si no se requiere información adicional.
        /// </summary>
        public TDetails? Details { get; set; } = null!;
    }
}
