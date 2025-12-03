using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Presentacion.Filters;
using System.Buffers;

namespace MqttBroker.API.Extencions
{
    /// <summary>
    /// Extensiones para configurar los controladores de la API con Newtonsoft.Json.
    /// </summary>
    public static class ApiConfigurationExtensions
    {
        /// <summary>
        /// Configura los controladores para usar camelCase, ignorar mayúsculas/minúsculas
        /// y evitar referencias circulares.
        /// </summary>
        public static void AddConfiguredApiControllers(
            this IServiceCollection services,
            ILogger logger,
            ObjectPoolProvider objectPoolProvider)
        {
            services.AddControllers(config =>
            {
                config.RespectBrowserAcceptHeader = true;
                config.ReturnHttpNotAcceptable = true;

                // Formateador consistente con JsonPatch
                config.InputFormatters.Insert(0, GetJsonPatchInputFormatter(logger, objectPoolProvider));

                // Filtros globales
                AddGlobalFilters(config);
            })
            .AddNewtonsoftJson(options =>
            {
                //Usa camelCase al serializar
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

                //Ignora mayúsculas/minúsculas al deserializar
                var namingStrategy = (options.SerializerSettings.ContractResolver as DefaultContractResolver)?.NamingStrategy;
                if (namingStrategy != null)
                {
                    namingStrategy.ProcessDictionaryKeys = true;
                    namingStrategy.OverrideSpecifiedNames = false;
                }

                //Evita referencias circulares
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                //Ignora valores nulos
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

                //Ignora propiedades de metadatos
                options.SerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
            })
            .AddApplicationPart(typeof(Presentacion.AssemblyReference).Assembly);
        }

        //Configuración coherente del formateador JsonPatch
        private static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter(
            ILogger logger,
            ObjectPoolProvider objectPoolProvider)
        {
            return new NewtonsoftJsonPatchInputFormatter(
                logger,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                },
                ArrayPool<char>.Shared,
                objectPoolProvider,
                new MvcOptions(),
                new MvcNewtonsoftJsonOptions());
        }

        private static void AddGlobalFilters(MvcOptions config)
        {
            
            config.Filters.Add<StandardResponseFilter>();
        }
    }
}
