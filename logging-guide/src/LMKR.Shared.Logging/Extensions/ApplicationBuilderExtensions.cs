using LMKR.Shared.Logging.Middleware;
using Microsoft.AspNetCore.Builder;

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
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            return app;
        }
    }
}
