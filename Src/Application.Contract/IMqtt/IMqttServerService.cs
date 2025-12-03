

namespace Application.Contract.IMqtt
{
    public interface IMqttServerService
    {
        Task<bool> SendEventToUserByChipIdAsync(string chipId, string message);
        Task StartAsync();
        Task StopAsync();
    }
}
