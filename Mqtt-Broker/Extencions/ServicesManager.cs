using Application.Contract.IServcies;
using Application.Services;

namespace MqttBroker.API.Extencions
{
    public static class ServicesManager
    {
        public static void AddServcies(this IServiceCollection services)
        {
            services.AddScoped<IFirmwareServices, FirmwareServices>();
            services.AddScoped<IDeviceServcies, DeviceServcies>();

        }

    }
}
