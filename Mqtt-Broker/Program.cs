using MqttBroker.API.Extencions;
using MqttBroker.API.Extensions;
using Application.Contract.IMqtt;
using Application.Mappers;
using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using Persitencia.Contexts;
using Presentacion.Middleware;
using Serilog;
using Shared;
using System.Runtime.InteropServices;


var basePath = AppContext.BaseDirectory;
var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureLogHost(builder.Configuration);


// Cargar configuración desde appsettings.json manualmente desde AppContext.BaseDirectory
builder.Configuration
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();


// Obtener cadena de conexión desde la configuración
var connectionString = builder.Configuration.GetConnectionString("MqttBrokerConnection");

// Add services to the container.
builder.Services.ConfigureLoggerService();
builder.Services.AddControllers();
builder.Services.ConfigureCors(builder.Configuration);
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.ConfigureApiVersioning(builder.Configuration);
builder.Services.AddMqttConfiguration(builder.Configuration);
builder.Services.AddServcies();
builder.Services.AddScalar();
builder.Services.AddPatternOptions(builder.Configuration);
builder.Services.AddRedisConnection(builder.Configuration);
builder.Services.ConfigureContextPostgreSql<MqttBrokerContext>(connectionString!);
builder.Services.ConfigureRepositoryManager();
builder.Services.ConfigureMqttStrategy();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(SupportedLanguages.En);
    options.SupportedCultures = SupportedLanguages.SupportedCultureInfos.ToList();
    options.SupportedUICultures = SupportedLanguages.SupportedCultureInfos.ToList();
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var serviceProvider = builder.Services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var objectPoolProvider = serviceProvider.GetRequiredService<ObjectPoolProvider>();
builder.Services.AddConfiguredApiControllers(logger, objectPoolProvider);

var app = builder.Build();

// Iniciar MQTT
var mqttService = app.Services.GetRequiredService<IMqttServerService>();
await mqttService.StartAsync();

var webRootPath = app.Environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");

if (!Directory.Exists(webRootPath))
{
    Directory.CreateDirectory(webRootPath);
}

// Configuración de archivos estáticos basada en el sistema operativo
if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "wwwroot")),
        RequestPath = "/static",
        OnPrepareResponse = ctx =>
        {
            // Configuración de caché para archivos estáticos
            const int durationInSeconds = 60 * 60 * 24 * 7; // 7 días
            ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={durationInSeconds}");

            // Bloquear acceso a la carpeta Templates
            if (ctx.File.PhysicalPath!.Contains(Path.Combine("wwwroot", "Templates")))
            {
                ctx.Context.Response.StatusCode = StatusCodes.Status403Forbidden;
                ctx.Context.Response.ContentLength = 0;
                ctx.Context.Response.Body = Stream.Null;
            }

        }
    });

}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    app.UseStaticFiles();
}

app.UseDefaultFiles();

app.UseCors("CorsPolicy");

app.UseRouting();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});

app.UseAuthentication();

app.UseAuthorization();

app.ConfigureMiddlewareApp();

// Enable the endpoint for generating the OpenAPI documents
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // expone /swagger/v1/swagger.json
}

app.MapControllers();

app.UseHangfireDashboard();

app.UseSerilogRequestLogging();

app.LogStartupInfo(app.Logger, connectionString!);

app.Run();


