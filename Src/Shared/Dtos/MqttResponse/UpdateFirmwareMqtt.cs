namespace Shared.Dtos.MqttResponse
{
    public class UpdateFirmwareMqtt
    {
        public string? FirmwareVersion { get; set; }
        public int PartIndex { get; set; }
        public int TotalParts { get; set; }
        public string? Base64Part { get; set; }
        public bool IsError { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }
}
