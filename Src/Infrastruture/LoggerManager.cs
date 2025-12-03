using Application.Contracts;
using Microsoft.Extensions.Logging;
using Shared.Utils;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Infrastructure
{
    public class LoggerManager<TService> : ILoggerManager<TService> where TService : class
    {
        private readonly ILogger<TService> _logger;

        private const int MaxLogMessageLength = 500; // Límite para el mensaje
        private const int MaxLogDataLength = 800;    // Límite para los datos

        private static readonly Regex SensitiveRegex = new(
            @"(\""(Password|NewPassword|ConfirmPassword|Token|AccessToken|RefreshToken|ApiKey|Secret|Key|Payment|CardNumber|Pan|Cvv|CardHolder|Expiration|Pin|Otp|Code|AuthCode)\""\s*:\s*\"")(.*?)(""\s*[,\}])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        public LoggerManager(ILogger<TService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static string SanitizeSensitiveData(object? data)
        {
            if (data == null) return string.Empty;

            string json;

            if (data is string s)
                json = s;
            else
            {
                try
                {
                    json = JsonSerializer.Serialize(data);
                }
                catch
                {
                    return data.ToString() ?? string.Empty;
                }
            }

            // Reemplazar los valores sensibles por ***
            return SensitiveRegex.Replace(json, "$1***$4");
        }

        private static string SanitizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return message!;
            return SensitiveRegex.Replace(message, "$1***$4");
        }

        public void LogDebug(string message, object? data = null)
        {
            message = SanitizeMessage(message).TruncateLongString(MaxLogMessageLength);
            data = SanitizeSensitiveData(data).TruncateLongString(MaxLogDataLength);
            _logger.LogDebug("[{Service}] {Message} | Data: {Data}", typeof(TService).Name, message, data);
        }

        public void LogError(string message, object? data = null)
        {
            message = SanitizeMessage(message).TruncateLongString(MaxLogMessageLength);
            data = SanitizeSensitiveData(data).TruncateLongString(MaxLogDataLength);
            _logger.LogError("[{Service}] {Message} | Data: {Data}", typeof(TService).Name, message, data);
        }

        public void LogInfo(string message, object? data = null)
        {
            message = SanitizeMessage(message).TruncateLongString(MaxLogMessageLength);
            data = SanitizeSensitiveData(data).TruncateLongString(MaxLogDataLength);
            _logger.LogInformation("[{Service}] {Message} | Data: {Data}", typeof(TService).Name, message, data);
        }

        public void LogWarn(string message, object? data = null)
        {
            message = SanitizeMessage(message).TruncateLongString(MaxLogMessageLength);
            data = SanitizeSensitiveData(data).TruncateLongString(MaxLogDataLength);
            _logger.LogWarning("[{Service}] {Message} | Data: {Data}", typeof(TService).Name, message, data);
        }
    }
}

