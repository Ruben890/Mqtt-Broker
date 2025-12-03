using Newtonsoft.Json;
using Shared.Dtos.TelemetryRecord;

namespace Shared.Dtos.MqttRequest
{
    public class TelemetryRecordRequest
    {
        [JsonProperty("telemetry")]
        public TelemetryRecordMqtt? TelemetryRecord { get; set; } = null!;
    }
}
