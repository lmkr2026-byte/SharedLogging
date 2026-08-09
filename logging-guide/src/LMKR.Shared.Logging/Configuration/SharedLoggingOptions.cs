namespace LMKR.Shared.Logging.Configuration
{
    /// <summary>
    /// Bound from the "SharedLogging" section of each service's appsettings.json.
    /// This is the ONLY place a service can tune behaviour; everything else
    /// (which viewer, DB targets, etc.) lives in config, not code, so a change
    /// never requires touching a service's source.
    /// </summary>
    public class SharedLoggingOptions
    {
        public const string SectionName = "SharedLogging";

        /// <summary>Logical name of the calling service, e.g. "ParcelManagement.API".
        /// Stamped on every log row/entry so 29 services can share one table/index
        /// and still be filtered individually.</summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>Optional override of the destination table name used by the
        /// shared logging stored procedure. Leave empty to use the default per-service
        /// table configured in the central logging DB.</summary>
        public string TargetTable { get; set; } = string.Empty;

        /// <summary>Named connection string (in ConnectionStrings section) that
        /// points at the central logging database.</summary>
        public string ConnectionStringName { get; set; } = "ApisLogsManagement";

        public bool HttpRequestLogging { get; set; } = true;

        public bool HttpResponseLogging { get; set; } = true;

        /// <summary>Log request/response bodies. Turn off per-service for
        /// endpoints carrying large payloads or sensitive data.</summary>
        public bool LogBody { get; set; } = true;

        /// <summary>Max characters of request/response body captured before
        /// truncation, to protect the logging DB from oversized payloads.</summary>
        public int MaxBodyLength { get; set; } = 8000;
    }
}
