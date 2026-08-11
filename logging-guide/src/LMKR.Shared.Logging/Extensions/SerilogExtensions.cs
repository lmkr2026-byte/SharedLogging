using LMKR.Shared.Logging.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace LMKR.Shared.Logging.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSharedSerilog(this WebApplicationBuilder builder)
    {
        //--------------------------------------------------
        // logging (configuration)
        //--------------------------------------------------

        var loggingOptions = builder.Configuration.GetSection(SharedLoggingOptions.SectionName).Get<SharedLoggingOptions>() ?? new SharedLoggingOptions();

        //--------------------------------------------------
        // Seq (configuration)
        //--------------------------------------------------
        var seqOptions = builder.Configuration.GetSection(SeqOptions.SectionName).Get<SeqOptions>() ?? new SeqOptions();


        builder.Host.UseSerilog((context, services, loggerConfig) =>
        {
            loggerConfig.ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", loggingOptions.ServiceName)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId();

            //--------------------------------------------------
            // Service-specific File Logs
            //--------------------------------------------------
            if (loggingOptions.EnableFileLogging)
            {
                var serviceFolder = Path.Combine(loggingOptions.LogRootPath, loggingOptions.ServiceName);
                var errorFolder = Path.Combine(serviceFolder, "Errors");
                Directory.CreateDirectory(serviceFolder);
                Directory.CreateDirectory(errorFolder);

                loggerConfig.WriteTo.File(Path.Combine(serviceFolder, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    rollOnFileSizeLimit: true);

                loggerConfig.WriteTo.File(Path.Combine(errorFolder, "error-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 90,
                    restrictedToMinimumLevel: LogEventLevel.Error,
                    shared: true,
                    rollOnFileSizeLimit: true);
            }

            //--------------------------------------------------
            // Seq (optional)
            //--------------------------------------------------
            if (loggingOptions.EnableSeq && !string.IsNullOrWhiteSpace(seqOptions.Url))
            {
                loggerConfig.WriteTo.Seq( serverUrl: seqOptions.Url, apiKey: string.IsNullOrWhiteSpace(seqOptions.ApiKey) ? null: seqOptions.ApiKey);
            }
        });

        return builder;
    }
}