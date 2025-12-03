using Application.Contract.IRedis;
using Application.Contract.IUnitOfWork;
using Infrastructure.Interfaces;
using Infrastructure.Mqtt;
using Infrastructure.Mqtt.MqttStrategies;
using Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.OpenApi.Models;
using Npgsql;
using Persitencia;
using Persitencia.Interfaces;
using Persitencia.UnitOfWork;
using Shared;

namespace MqttBroker.API.Extencions
{
    public static class ServiceExtensions
    {
        public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
        {
            // Leer la lista de orígenes permitidos desde la configuración
            var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();

            if (allowedOrigins is null || allowedOrigins.Length == 0)
            {
                throw new InvalidOperationException("No allowed origins have been defined in the settings. Make sure to add the 'AllowedOrigins' section in appsettings.");
            }

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyHeader() // Permitir todos los encabezados
                           .AllowAnyMethod() // Permitir todos los métodos (GET, POST, PUT, DELETE, etc.)
                           .AllowCredentials() // Habilitar credenciales
                           .WithExposedHeaders("X-Custom-Header") // Exponer solo los encabezados necesarios
                           .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cacheo de preflight (OPTIONS)

                });
            });
        }

        public static void ConfigureIISIntegration(this IServiceCollection services) =>
            services.Configure<IISOptions>(options => { });


        public static void AddPatternOptions(this IServiceCollection services, IConfiguration configuration)
        {
            // Configuración de MQTT
            services.Configure<MqttSettings>(
                configuration.GetSection("MqttSettings")
            );
        }

        public static void AddScalar(this IServiceCollection services)
        {
            // Swashbuckle config
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Mqtt-Broker",
                    Version = "v1"
                });

                // 🔥 Agrega definición de seguridad tipo ApiKey
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "x-api-key",
                    Type = SecuritySchemeType.ApiKey,
                    Description = "Clave de acceso requerida en el header x-api-key"
                });

                // 🔒 Aplica la definición a todos los endpoints
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                c.UseInlineDefinitionsForEnums();
                c.EnableAnnotations();
            });

            // NSwag opcional
            services.AddOpenApiDocument(config =>
            {
                config.Title = "MqttBroker API";
                config.Version = "v1";
                config.AddSecurity("ApiKey", new NSwag.OpenApiSecurityScheme
                {
                    Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
                    Name = "x-api-key",
                    In = NSwag.OpenApiSecurityApiKeyLocation.Header,
                    Description = "Clave de acceso requerida en el header x-api-key"
                });

                config.OperationProcessors.Add(
                    new NSwag.Generation.Processors.Security.AspNetCoreOperationSecurityScopeProcessor("ApiKey"));
            });
        }


        public static void AddRedisConnection(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Registramos un servicio que decidirá qué Redis usar
            services.AddSingleton<IRedisService, RedisService>();
            services.AddSingleton<IRedisClientMapping, RedisClientMapping>();
        }


        public static void ConfigureContextPostgreSql<TContext>(
             this IServiceCollection services,
             string connectionString,
             string migrationsTableName = "__EFMigrationsHistory",
             string? schemaName = null)
             where TContext : DbContext
        {
            services.AddDbContext<TContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.UseNetTopologySuite(); // <-- Esto es clave
                    npgsqlOptions.MigrationsAssembly(typeof(TContext).Assembly.FullName);

                    if (!string.IsNullOrEmpty(schemaName))
                    {
                        npgsqlOptions.MigrationsHistoryTable(migrationsTableName, schemaName);
                    }
                });

                options.ConfigureWarnings(warnings =>
                    warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning)
                );
            });
        }


        public static void ConfigureMqttStrategy(this IServiceCollection services)
        {
            services.AddTransient<IMqttStrategy, StatusStrategy>();
            services.AddTransient<IMqttStrategy, TelemetryStrategy>();
            services.AddTransient<IMqttStrategy, ErrorUpdateFirmwareDeviceStrategy>();
            services.AddScoped<IMqttStrategyResolver, MqttStrategyResolver>();
            services.AddSingleton<MqttMessageProcessor>();

        }


        public static void ConfigureRepositoryManager(this IServiceCollection services)
        {
            services.AddScoped<IMqttBrokerUnitOfWorkManager, MqttBrokerUnitOfWorkManager>();
            services.AddScoped<IRepositoryFactory, RepositoryFactory>();
        }


    }
}
