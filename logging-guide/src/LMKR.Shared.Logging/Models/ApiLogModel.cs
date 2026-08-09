namespace LMKR.Shared.Logging.Models
{
    /// <summary>
    /// One row = one API call (request fields filled on insert, response
    /// fields filled on the follow-up update). Shared across all 29 services;
    /// ServiceName is what lets you filter/aggregate per service.
    /// </summary>
    public class ApiLogModel
    {
        public long Id { get; set; }

        public string ServiceName { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string? ClientId { get; set; }

        /// <summary>Optional routing hint (e.g. "Error", "Audit") consulted by
        /// usp_ApiLogs_Save/dbo.LogRoutingConfig. Leave null for the default
        /// per-service routing.</summary>
        public string? LogCategory { get; set; }

        public string APIMethod { get; set; } = string.Empty;
        public string APIURL { get; set; } = string.Empty;
        public string? RequestBody { get; set; }
        public string? ResponseBody { get; set; }
        public int? StatusCode { get; set; }
        public long? DurationMs { get; set; }

        public string? ClientIP { get; set; }
        public string? UserAgent { get; set; }

        public long CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
    }

    public class ApiLogResponseModel
    {
        public long Id { get; set; }
        public long ErrorCode { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
