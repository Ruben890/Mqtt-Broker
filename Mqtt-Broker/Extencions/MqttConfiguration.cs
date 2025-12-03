using Application.Contract.IMqtt;
using Infrastructure.Mqtt;
using MQTTnet;
using MQTTnet.AspNetCore;

namespace MqttBroker.API.Extensions
{
    public static class MqttConfiguration
    {
        public static IServiceCollection AddMqttConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var mqttSettings = configuration.GetSection("MqttSettings");
            var port = mqttSettings.GetValue<int>("Port", 1883); // TCP port por defecto

            services.AddMqttServer(options =>
            {
                options.WithDefaultEndpointPort(port);
                options.WithDefaultEndpoint();
            })
            .AddMqttConnectionHandler()
            .AddMqttTcpServerAdapter();

            // Registrar un servicio para manejar eventos del servidor
            services.AddSingleton<IMqttServerService, MqttServerService>();
            
            return services;
        }
    }
}
