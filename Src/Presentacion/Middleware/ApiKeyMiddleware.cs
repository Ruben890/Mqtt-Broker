using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Presentacion.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private readonly HashSet<string> _allowedOrigins;
        private readonly List<string> _excludedPaths;
        private readonly string _apiKey;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration config, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _logger = logger;

            _allowedOrigins = config.GetSection("AllowedOrigins")
                                    .Get<HashSet<string>>() ?? new HashSet<string>();

            _excludedPaths = config.GetSection("ExcludedPaths")
                                   .Get<List<string>>() ?? new List<string>();

            _apiKey = config.GetValue<string>("ApiKeys")
                      ?? throw new ArgumentNullException("ApiKeys", "API key is not configured.");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var origin = context.Request.Headers["Origin"].ToString();

            // Saltar validación si la ruta coincide con exacta o comodín /* al inicio
            if (_excludedPaths.Any(p => MatchesExcludedPath(path, p)))
            {
                await _next(context);
                return;
            }

            // Si el origen está permitido, dejamos pasar sin validar API Key
            if (!string.IsNullOrEmpty(origin) && _allowedOrigins.Contains(origin))
            {
                await _next(context);
                return;
            }

            // Validación de API Key
            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedApiKey)
                || !string.Equals(extractedApiKey, _apiKey, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Rejected request due to invalid API key from origin: {Origin}, path: {Path}", origin, path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API Key.");
                return;
            }

            await _next(context);
        }

        private bool MatchesExcludedPath(string path, string pattern)
        {
            // Normalizamos solo la ruta
            var normalizedPath = path.TrimEnd('/');

            bool isMatch = false;

            if (pattern.EndsWith("*"))
            {
                var prefix = pattern.Substring(0, pattern.Length - 1).TrimEnd('/'); // quitar * y barra final del prefijo
                isMatch = normalizedPath.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                          normalizedPath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                isMatch = normalizedPath.Equals(pattern.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
            }
            return isMatch;
        }
    }
}
