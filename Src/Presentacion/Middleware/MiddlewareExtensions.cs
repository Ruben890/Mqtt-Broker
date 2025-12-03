using Microsoft.AspNetCore.Builder;

namespace Presentacion.Middleware
{
    public static class MiddlewareExtensions
    {
        public static void ConfigureMiddlewareApp(this IApplicationBuilder app)
        {
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseMiddleware<RequestCultureMiddleware>();
            app.UseMiddleware<ApiKeyMiddleware>();
        }
    }
}
