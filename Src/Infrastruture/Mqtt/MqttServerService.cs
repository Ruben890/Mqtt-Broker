using Application.Contract.IMqtt;
using Application.Contract.IRedis;
using Application.Contracts;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Server;
using Shared;

namespace Infrastructure.Mqtt
{
    public sealed partial class MqttServerService : IMqttServerService
    {
        private readonly MqttServer _mqttServer;
        private readonly MqttSettings _settings;
        private readonly ILoggerManager<MqttServerService> _logger;
        private readonly IRedisClientMapping _clientMapping;
        private readonly MqttMessageProcessor _messageProcessor;

        public MqttServerService(
            IOptions<MqttSettings> options,
            ILoggerManager<MqttServerService> logger,
            MqttServer mqttServer,
            IRedisClientMapping clientMapping,
            MqttMessageProcessor messageProcessor)
        {
            _mqttServer = mqttServer;
            _clientMapping = clientMapping;
            _settings = options.Value;
            _logger = logger;
            _messageProcessor = messageProcessor;

            // Suscribirse a eventos del broker
            _mqttServer.ClientConnectedAsync += OnClientConnected;
            _mqttServer.ClientDisconnectedAsync += OnClientDisconnected;
            _mqttServer.StartedAsync += OnServerStarted;
            _mqttServer.ValidatingConnectionAsync += OnClientConnecting;
            _mqttServer.InterceptingPublishAsync += OnMessageIntercepted;
            _mqttServer.ClientSubscribedTopicAsync += OnClientSubscribed;
            _mqttServer.ClientUnsubscribedTopicAsync += OnClientUnsubscribed;
        }


        /// <summary>
        /// Publica un evento a un ChipId específico
        /// </summary>
        public async Task<bool> SendEventToUserByChipIdAsync(string chipId, string message)
        {
            if (string.IsNullOrEmpty(chipId))
            {
                _logger.LogWarn("No ChipId was provided to send the event.");
                return false;
            }

            var clientId = await _clientMapping.GetClientAsync(chipId);

            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogWarn($"No ClientId found associated with ChipId {chipId}.");
                return false; // <-- importante
            }

            var topic = $"event/{chipId}";
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(true)
                .Build();

            await _mqttServer.InjectApplicationMessage(
                new InjectedMqttApplicationMessage(mqttMessage)
                {
                    SenderClientId = "internal-server"
                });

            _logger.LogInfo($"[ServerEvent] Event sent to ChipId={chipId}, ClientId={clientId}, Topic={topic}");
            return true;
        }



        public Task StartAsync() => _mqttServer.StartAsync();
        public Task StopAsync() => _mqttServer.StopAsync();

    }
}
