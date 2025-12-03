using Application.Contracts;
using Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Server;
using Shared.Enums;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

public class MqttMessageProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILoggerManager<MqttMessageProcessor> _logger;

    // Canal para procesar mensajes de forma controlada
    private readonly Channel<(string Topic, string Payload)> _messageChannel;

    // Cache para mapping topicSegment -> MqttEventType
    private readonly ConcurrentDictionary<string, MqttEventType?> _topicCache = new();

    public MqttMessageProcessor(IServiceScopeFactory scopeFactory, ILoggerManager<MqttMessageProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Canal con capacidad limitada para evitar saturación
        _messageChannel = Channel.CreateBounded<(string, string)>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        // Lanzar el procesador de mensajes en segundo plano
        _ = ProcessMessagesAsync();
    }

    /// <summary>
    /// Método a llamar desde el interceptor.
    /// Solo encola los mensajes para procesarlos de forma asincrónica.
    /// </summary>
    public Task OnMessageIntercepted(InterceptingPublishEventArgs e)
    {
        try
        {
            var topic = e.ApplicationMessage.Topic;

            string payload = e.ApplicationMessage.Payload.Length > 0
                ? Encoding.UTF8.GetString(e.ApplicationMessage.Payload)
                : "<empty>";

            // Enviar al canal para procesar
            _messageChannel.Writer.TryWrite((topic, payload));

            _logger.LogInfo($"Message queued: Topic={topic}, Payload={payload.Replace("{", "{{").Replace("}", "}}")}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error queueing intercepted message: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Procesa los mensajes encolados de forma controlada y asincrónica.
    /// Crea un nuevo scope por cada mensaje para resolver dependencias Scoped.
    /// </summary>
    private async Task ProcessMessagesAsync()
    {
        await foreach (var (topic, payload) in _messageChannel.Reader.ReadAllAsync())
        {
            try
            {
                // Extraer el último segmento del topic (por ejemplo "status" o "telemetry")
                var segment = topic.Split('/').Last();

                // Normalizar el segmento: quitar guiones y pasarlo a PascalCase
                string NormalizeSegment(string s)
                {
                    var parts = s.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                    return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant()));
                }

                var normalizedSegment = NormalizeSegment(segment);

                var eventType = _topicCache.GetOrAdd(normalizedSegment.ToLowerInvariant(), key =>
                {
                    // Intentar parsear directamente
                    if (Enum.TryParse<MqttEventType>(normalizedSegment, true, out var parsed))
                        return parsed;

                    // Si no existe, puedes devolver un valor por defecto o null
                    return null;
                });

                _logger.LogInfo($"[MqttMessageProcessor] Processing event: {eventType} | Topic: {topic}");

                //Crear un scope nuevo para cada mensaje
                using var scope = _scopeFactory.CreateScope();

                var resolver = scope.ServiceProvider.GetRequiredService<IMqttStrategyResolver>();

                // Ejecutar la estrategia dentro del scope
                await resolver.ResolveAsync(eventType.Value, topic, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MqttMessageProcessor] Error processing message from topic {topic}: {ex.Message}", ex);
            }
        }
    }
}
