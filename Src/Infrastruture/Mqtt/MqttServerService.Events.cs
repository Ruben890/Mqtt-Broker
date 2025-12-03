using MQTTnet.Server;

namespace Infrastructure.Mqtt
{
    public sealed partial class MqttServerService
    {


        /// <summary>
        /// Event: Client subscribed to a topic.
        /// </summary>
        private Task OnClientSubscribed(ClientSubscribedTopicEventArgs e)
        {
            _logger.LogInfo($"Client '{e.ClientId}' subscribed to topic '{e.TopicFilter.Topic}'");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Event: Client unsubscribed from a topic.
        /// </summary>
        private Task OnClientUnsubscribed(ClientUnsubscribedTopicEventArgs e)
        {
            _logger.LogInfo($"Client '{e.ClientId}' unsubscribed from topic '{e.TopicFilter}'");
            return Task.CompletedTask;
        }

        // -----------------------------
        /// <summary>
        /// Evento: Cliente conectado exitosamente al servidor.
        /// </summary>
        private Task OnClientConnected(ClientConnectedEventArgs e)
        {
            _logger.LogInfo($"Client connected: {e.ClientId}");
            return Task.CompletedTask;
        }

        // -----------------------------
        /// <summary>
        /// Evento: Cliente desconectado del servidor.
        /// </summary>
        private Task OnClientDisconnected(ClientDisconnectedEventArgs e)
        {
            _logger.LogInfo($"Client disconnected: {e.ClientId}");
            return Task.CompletedTask;
        }

        // -----------------------------
        /// <summary>
        /// Evento: Servidor MQTT iniciado.
        /// </summary>
        private Task OnServerStarted(EventArgs e)
        {
            _logger.LogInfo("MQTT server started and ready for connections.");

            return Task.CompletedTask;
        }

        private Task OnMessageIntercepted(InterceptingPublishEventArgs e)
        {
            return _messageProcessor.OnMessageIntercepted(e);
        }

        // -----------------------------
        /// <summary>
        /// Evento: Validación de conexión del cliente.
        /// Soporta MQTT 5.0 (UserProperties) y legacy 3.1.1.
        /// </summary>
        private async Task OnClientConnecting(ValidatingConnectionEventArgs e)
        {
            string apiKey;
            string chipId;

            // Selecciona método según versión del protocolo
            if (e.ProtocolVersion == MQTTnet.Formatter.MqttProtocolVersion.V500)
            {
                (apiKey, chipId) = HandleMqtt5Client(e);
            }
            else
            {
                (apiKey, chipId) = HandleMqttLegacyClient(e);
            }

            // Validar que se haya proporcionado API Key
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarn($"Client rejected: {e.ClientId}, API Key not provided.");
                e.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.NotAuthorized;
                return;
            }

            // Verificar API Key contra la lista de configuraciones
            bool validApiKey = _settings.ApiKeys != null &&
                               _settings.ApiKeys.Any(dict => dict.Values.Contains(apiKey));

            if (!validApiKey)
            {
                _logger.LogWarn($"Client rejected: {e.ClientId}, invalid API Key: {apiKey}");
                e.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.NotAuthorized;
                return;
            }

            // Registrar ChipId si no está definido
            if (string.IsNullOrWhiteSpace(chipId))
            {
                chipId = e.ClientId; // fallback
            }

            await _clientMapping.SetOrUpdateClientAsync(chipId, e.ClientId);
            _logger.LogInfo($"Client connected: {e.ClientId}, API Key validated, ChipId={chipId}");

            e.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;
        }


        // -----------------------------
        /// <summary>
        /// Maneja clientes MQTT 5.0 usando UserProperties.
        /// ApiKey y ChipId se envían como propiedades.
        /// </summary>
        private (string apiKey, string chipId) HandleMqtt5Client(ValidatingConnectionEventArgs e)
        {
            string apiKey = e.UserProperties?
                .FirstOrDefault(p => string.Equals(p.Name, "ApiKey", StringComparison.OrdinalIgnoreCase))
                ?.Value!;

            string chipId = e.UserProperties?
                .FirstOrDefault(p => string.Equals(p.Name, "ChipId", StringComparison.OrdinalIgnoreCase))
                ?.Value!;

            _logger.LogInfo($"Client {e.ClientId} attempting MQTT 5.0 connection.");
            return (apiKey, chipId);
        }

        // -----------------------------
        /// <summary>
        /// Maneja clientes legacy MQTT <5 (3.1.1)
        /// Arduino envía:
        /// ClientId = ChipId
        /// Username = ApiKey
        /// Password = vacío
        /// </summary>
        private (string apiKey, string chipId) HandleMqttLegacyClient(ValidatingConnectionEventArgs e)
        {
            string apiKey = e.UserName;   // Username contiene la API Key
            string chipId = e.ClientId;   // ClientId se usa como ChipId

            _logger.LogInfo($"Client {e.ClientId} using legacy authentication (MQTT <5), ChipId={chipId}");
            return (apiKey, chipId);
        }
    }
}
