using Microsoft.AspNetCore.Http;

namespace Presentacion.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {

            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-XSS-Protection"] = "0";
            headers["X-Frame-Options"] = "DENY";

            headers["Content-Security-Policy"] = string.Join("; ",
                "default-src 'self'",
                "script-src 'self'",
                "style-src 'self'",
                "img-src 'self' data:",
                "font-src 'self'",
                "object-src 'none'",
                "frame-ancestors 'none'",
                "base-uri 'none'",
                "form-action 'self'"
            );

            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), fullscreen=(), payment=(), usb=()";

            await _next(context);
        }
    }

}
