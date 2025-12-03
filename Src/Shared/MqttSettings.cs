namespace Shared
{
    public class MqttSettings
    {
        public int Port { get; set; } = 8083;
        public List<Dictionary<string, string>>? ApiKeys { get; set; } = new();
    }
}
