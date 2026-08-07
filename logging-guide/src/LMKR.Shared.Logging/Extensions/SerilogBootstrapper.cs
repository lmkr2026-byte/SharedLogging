using Microsoft.Extensions.Configuration;
using Serilog;

namespace LMKR.Shared.Logging.Extensions
{
    /// <summary>
    /// Builds the Serilog pipeline from config alone. Every sink package
    /// (Seq, Elasticsearch, Application Insights, File, Console) ships as a
    /// dependency of this shared library, so a service switches its "viewer"
    /// by editing the Serilog:WriteTo array in appsettings.{Environment}.json
    /// - no code change, no redeploy of anything but config, in any of the
    /// 29 services. See /samples/appsettings.sample.json for every option.
    /// </summary>
    public static class SerilogBootstrapper
    {
        /// <summary>
        /// Configures the LoggerConfiguration instance handed to you by
        /// <c>Host.UseSerilog((context, services, loggerConfig) =&gt; ...)</c>
        /// IN PLACE (Serilog's fluent API mutates and returns the same
        /// instance), and returns it back for convenience. Call this instead
        /// of building your own LoggerConfiguration so every service gets
        /// identical enrichment; only <paramref name="serviceName"/> differs.
        /// </summary>
        public static LoggerConfiguration Configure(
            LoggerConfiguration loggerConfig, IConfiguration configuration, string serviceName)
        {
            return loggerConfig
                .ReadFrom.Configuration(configuration)      // reads the "Serilog" section - sinks, min levels, overrides
                .Enrich.FromLogContext()                    // picks up CorrelationId/ClientId/Service pushed by the middleware
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId();
        }
    }
}
