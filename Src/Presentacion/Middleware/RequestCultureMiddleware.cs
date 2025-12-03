using Microsoft.AspNetCore.Http;
using Shared;
using System.Globalization;

namespace Presentacion.Middleware
{
    public class RequestCultureMiddleware
    {
        private readonly RequestDelegate _next;

        private const string DefaultLanguage = SupportedLanguages.En;
        private static readonly string ErrorMessage = $"Invalid language parameter. Supported languages are: {string.Join(", ", SupportedLanguages.SupportedCultures)}";

        public RequestCultureMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Buscar el parámetro 'lang' sin importar mayúsculas
                string? langParam = context.Request.Query
                    .FirstOrDefault(p => p.Key.Equals("lang", StringComparison.OrdinalIgnoreCase)).Value
                    .FirstOrDefault();

                string? lang = ValidateLanguageParam(langParam);

                if (string.IsNullOrWhiteSpace(lang))
                {
                    lang = GetLanguageFromHeader(context.Request.Headers["Accept-Language"].FirstOrDefault());
                }

                lang ??= DefaultLanguage;

                if (!IsSupportedLanguage(lang))
                {
                    await RespondWithError(context, StatusCodes.Status400BadRequest, ErrorMessage);
                    return;
                }

                SetCurrentCulture(lang);
                await _next(context);
            }
            catch (CultureNotFoundException)
            {
                await RespondWithError(context, StatusCodes.Status400BadRequest, ErrorMessage);
            }
            catch (Exception)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("An unexpected error occurred while processing language settings.");
            }
        }

        private static async Task RespondWithError(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(message);
        }

        private static string? ValidateLanguageParam(string? languageParam)
        {
            if (string.IsNullOrWhiteSpace(languageParam))
                return null;

            languageParam = languageParam.Trim().ToLowerInvariant();

            return languageParam.All(char.IsLetter) ? languageParam : null;
        }

        private static string? GetLanguageFromHeader(string? acceptLanguageHeader)
        {
            if (string.IsNullOrWhiteSpace(acceptLanguageHeader))
                return null;

            try
            {
                return acceptLanguageHeader
                    .Split(',') // split multiple accepted languages
                    .FirstOrDefault()?
                    .Split(';')[0] // remove quality values
                    .Split('-')[0] // keep only language part (e.g. "es-ES" -> "es")
                    .Trim()
                    .ToLowerInvariant();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsSupportedLanguage(string? lang)
        {
            return !string.IsNullOrWhiteSpace(lang) &&
                   SupportedLanguages.SupportedCultures.Contains(lang, StringComparer.OrdinalIgnoreCase);
        }

        private static void SetCurrentCulture(string lang)
        {
            var culture = new CultureInfo(lang);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
    }
}
