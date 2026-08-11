using LMKR.Shared.Logging.Configuration;
using LMKR.Shared.Logging.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LMKR.Shared.Logging.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// One-line opt-in for a service: app.UseSharedLogging();
        /// Wires up correlation-id propagation + request/response logging in
        /// the correct order. Call this early in the pipeline, right after
        /// UseRouting/UseExceptionHandler and before auth/controllers.
        /// </summary>
        public static IApplicationBuilder UseSharedLogging(this IApplicationBuilder app)
        {
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();

            app.UseMiddleware<CorrelationIdMiddleware>();
            var loggingOptions = app.ApplicationServices
           .GetRequiredService<IOptions<SharedLoggingOptions>>()
           .Value;
            //Enable custom request / response logging
            if (loggingOptions.HttpRequestLogging || loggingOptions.HttpResponseLogging)
            {
                app.UseMiddleware<RequestResponseLoggingMiddleware>();
            }
            return app;
        }
    }
}
