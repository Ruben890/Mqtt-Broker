using Application.Contracts;
using Infrastructure;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

namespace MqttBroker.API.Extencions
{
    public static class LogConfiguration
    {
        private const string TextOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Exception}";

        public static void ConfigureLoggerService(this IServiceCollection services)
        {
            // Registro del logger con manejo apropiado de ciclo de vida
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });

            services.AddSingleton(typeof(ILoggerManager<>), typeof(LoggerManager<>));
        }

        public static void ConfigureLogHost(this IHostBuilder host, IConfiguration configuration)
        {
            host.UseSerilog((context, services, config) =>
            {
                var environment = context.HostingEnvironment;
                var logPath = Path.Combine(AppContext.BaseDirectory, "Logs");
                Directory.CreateDirectory(logPath);

                config.MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .Enrich.WithProperty("Application", configuration["AppName"])
                    .Enrich.FromLogContext();

                if (environment.IsDevelopment())
                {
                    config.WriteTo.Console(
                            theme: AnsiConsoleTheme.Code,
                            outputTemplate: TextOutputTemplate)
                          .WriteTo.Debug(restrictedToMinimumLevel: LogEventLevel.Debug)
                          .WriteTo.File(
                              path: Path.Combine(logPath, "debug-.log"),
                              retainedFileCountLimit: 7,
                              rollOnFileSizeLimit: true,
                              shared: true,
                              restrictedToMinimumLevel: LogEventLevel.Debug,
                              rollingInterval: RollingInterval.Day);
                }
                else
                {
                    config.WriteTo.Console(new CompactJsonFormatter());
                }

                config.WriteTo.File(
                    path: Path.Combine(logPath, "application-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    outputTemplate: TextOutputTemplate);

                config.WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: Path.Combine(logPath, "structured-logs-.json"),
                    retainedFileCountLimit: 30,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    rollingInterval: RollingInterval.Day);
            });
        }
    }
}
