using LMKR.Shared.Logging.Configuration;
using LMKR.Shared.Logging.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LMKR.Shared.Logging.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// One-line opt-in for a service: services.AddSharedLogging(builder.Configuration);
        /// Registers the options (bound from "SharedLogging" section) and the
        /// repository that talks to the single shared stored procedure.
        /// </summary>
        public static IServiceCollection AddSharedLogging(this IServiceCollection services, IConfiguration configuration)
        {
            var logsSection = configuration.GetSection(SharedLoggingOptions.SectionName);
            services.Configure<SharedLoggingOptions>(logsSection);
            services.AddTransient<IApiLoggingRepository, ApiLoggingRepository>();
            return services;
        }
    }
}
