using System.Reflection;

namespace MqttBroker.API.Extencions
{
    public static class StartupLoggerExtensions
    {
        private static readonly string[] SensitiveConnectionStringKeys =
            new[] { "User Id", "Password", "Uid","Username", "Pwd", "UserID", "user", "pass",
                   "Credential", "Credentials", "AccessKey", "SecretKey", "Token" };

        // Ahora recibe uno o más connection strings ya leídos, no nombres
        public static void LogStartupInfo(this WebApplication app, ILogger logger,
                                          params string[] connectionStrings)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            try
            {
                var env = app.Environment;

                var assemblyInfo = GetAssemblyInfo();
                var urlInfo = GetUrlInfo(app, app.Configuration);
                var processInfo = GetProcessInfo();

                logger.LogInformation("----------------------------------------------------");
                logger.LogInformation("Application Startup Summary");
                logger.LogInformation("----------------------------------------------------");
                logger.LogInformation("Project Name           : {ProjectName}", assemblyInfo.ProjectName);
                logger.LogInformation("Build Version          : {BuildVersion}", assemblyInfo.BuildVersion);
                logger.LogInformation("Informational Version  : {InformationalVersion}", assemblyInfo.InformationalVersion);
                logger.LogInformation("Started At             : {StartTime:yyyy-MM-dd HH:mm:ss.fff}", DateTime.Now);
                logger.LogInformation("Environment            : {Env}", env.EnvironmentName);
                logger.LogInformation("Content Root Path      : {ContentRoot}", env.ContentRootPath);
                logger.LogInformation("Process ID             : {ProcessId}", processInfo.Id);
                logger.LogInformation("Base Directory         : {BaseDirectory}", AppContext.BaseDirectory);
                logger.LogInformation("URLs from configuration: {ConfigUrls}", urlInfo.ConfigUrls);

                LogConnectionStrings(logger, connectionStrings);

                logger.LogInformation("----------------------------------------------------");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while logging startup information");
            }
        }

        private static (string ProjectName, string BuildVersion, string InformationalVersion) GetAssemblyInfo()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var assemblyName = entryAssembly?.GetName();

            return (
                assemblyName?.Name ?? "Unknown Project",
                assemblyName?.Version?.ToString() ?? "Unknown Version",
                entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "N/A"
            );
        }

        private static (string AppUrls, string ConfigUrls) GetUrlInfo(WebApplication app, IConfiguration config)
        {
            return (
                app.Urls.Any() ? string.Join(", ", app.Urls) : "not set in app.Urls",
                config["urls"] ?? config["ASPNETCORE_URLS"] ?? "not set in config"
            );
        }

        private static (int Id, string Name) GetProcessInfo()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            return (process.Id, process.ProcessName);
        }

        // Recibe las connection strings, no los nombres
        private static void LogConnectionStrings(ILogger logger, string[] connectionStrings)
        {
            if (connectionStrings == null || connectionStrings.Length == 0)
            {
                logger.LogInformation("No connection strings were provided to log.");
                return;
            }

            logger.LogInformation("Database Connections:");

            foreach (var connString in connectionStrings)
            {
                if (string.IsNullOrWhiteSpace(connString))
                {
                    logger.LogInformation("Empty or null connection string provided.");
                    continue;
                }

                var sanitizedConn = SanitizeConnectionString(connString);
                logger.LogInformation("{Connection}", sanitizedConn);
            }
        }

        private static string SanitizeConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            try
            {
                var builder = new System.Data.Common.DbConnectionStringBuilder
                {
                    ConnectionString = connectionString
                };

                foreach (var key in SensitiveConnectionStringKeys)
                {
                    if (builder.ContainsKey(key))
                    {
                        builder[key] = "****";
                    }
                }

                return builder.ConnectionString;
            }
            catch
            {
                var sanitized = connectionString;
                foreach (var key in SensitiveConnectionStringKeys)
                {
                    var pattern = $"{key}=";
                    var index = sanitized.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        var end = sanitized.IndexOf(';', index);
                        if (end == -1) end = sanitized.Length;
                        sanitized = sanitized.Remove(index, end - index)
                                     .Insert(index, $"{pattern}****");
                    }
                }
                return sanitized;
            }
        }
    }
}
