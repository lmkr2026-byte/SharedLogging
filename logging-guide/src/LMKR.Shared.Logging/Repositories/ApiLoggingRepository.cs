using System.Data;
using LMKR.Shared.Logging.Configuration;
using LMKR.Shared.Logging.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LMKR.Shared.Logging.Repositories
{
    /// <summary>
    /// Talks to exactly ONE stored procedure - usp_ApiLogs_Save - for every
    /// service. The procedure itself decides which physical table the row
    /// belongs in (see /sql/02_usp_ApiLogs_Save.sql); this class never needs
    /// to know or care about that mapping.
    /// </summary>
    public class ApiLoggingRepository : IApiLoggingRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<ApiLoggingRepository> _logger;

        public ApiLoggingRepository(
            IConfiguration configuration,
            IOptions<SharedLoggingOptions> options,
            ILogger<ApiLoggingRepository> logger)
        {
            _connectionString = configuration.GetConnectionString(options.Value.ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{options.Value.ConnectionStringName}' is not configured.");
            _logger = logger;
        }

        public Task<ApiLogResponseModel> LogRequestAsync(ApiLogModel model) => SaveAsync(model, "INSERT");

        public Task<ApiLogResponseModel> LogResponseAsync(ApiLogModel model) => SaveAsync(model, "UPDATE");

        private async Task<ApiLogResponseModel> SaveAsync(ApiLogModel model, string action)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await using var command = new SqlCommand("usp_ApiLogs_Save", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 5 // logging must never hang the request pipeline
                };

                command.Parameters.Add(new SqlParameter("@Action", SqlDbType.VarChar, 10) { Value = action });
                command.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = model.Id == 0 ? DBNull.Value : model.Id });
                command.Parameters.Add(new SqlParameter("@ServiceName", SqlDbType.NVarChar, 100) { Value = model.ServiceName });
                command.Parameters.Add(new SqlParameter("@CorrelationId", SqlDbType.NVarChar, 100) { Value = model.CorrelationId });
                command.Parameters.Add(new SqlParameter("@ClientId", SqlDbType.NVarChar, 100) { Value = (object?)model.ClientId ?? DBNull.Value });
                command.Parameters.Add(new SqlParameter("@APIMethod", SqlDbType.NVarChar, 10) { Value = (object?)model.APIMethod ?? DBNull.Value });
                // Sizes below must match the SP's declared parameter sizes (and the
                // table columns) exactly - SqlClient silently truncates a value when
                // converting to a shorter proc parameter, it does not raise an error.
                command.Parameters.Add(new SqlParameter("@APIURL", SqlDbType.NVarChar, 2048) { Value = (object?)model.APIURL ?? DBNull.Value });
                command.Parameters.Add(new SqlParameter("@RequestBody", SqlDbType.NVarChar, -1) { Value = (object?)model.RequestBody ?? DBNull.Value });
                command.Parameters.Add(new SqlParameter("@ResponseBody", SqlDbType.NVarChar, -1) { Value = (object?)model.ResponseBody ?? DBNull.Value });
                command.Parameters.Add(new SqlParameter("@StatusCode", SqlDbType.Int) { Value = (object?)model.StatusCode ?? DBNull.Value });
                command.Parameters.Add(new SqlParameter("@DurationMs", SqlDbType.BigInt) { Value = (object?)model.DurationMs ?? DBNull.Value });
                command.Parameters.Add(new SqlParameter("@ClientIP", SqlDbType.NVarChar, 50) { Value = (object?)model.ClientIP ?? DBNull.Value });
                command.Parameters.Add(new SqlParameter("@UserAgent", SqlDbType.NVarChar, 512) { Value = (object?)model.UserAgent ?? DBNull.Value });
                command.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.BigInt) { Value = model.CreatedBy });
                command.Parameters.Add(new SqlParameter("@UpdatedBy", SqlDbType.BigInt) { Value = (object?)model.UpdatedBy ?? DBNull.Value });
                command.Parameters.Add(new SqlParameter("@LogCategory", SqlDbType.NVarChar, 50) { Value = (object?)model.LogCategory ?? DBNull.Value });

                var idOut = new SqlParameter("@OutId", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
                var errorCodeOut = new SqlParameter("@ErrorCode", SqlDbType.BigInt) { Direction = ParameterDirection.Output };
                var errorMessageOut = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, -1) { Direction = ParameterDirection.Output };
                command.Parameters.Add(idOut);
                command.Parameters.Add(errorCodeOut);
                command.Parameters.Add(errorMessageOut);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                var result = new ApiLogResponseModel
                {
                    Id = idOut.Value is DBNull ? 0 : Convert.ToInt64(idOut.Value),
                    ErrorCode = errorCodeOut.Value is DBNull ? 0 : Convert.ToInt64(errorCodeOut.Value),
                    ErrorMessage = errorMessageOut.Value as string ?? string.Empty
                };

                // The SP swallows its own SQL errors into @ErrorCode/@ErrorMessage
                // instead of throwing, so ExecuteNonQueryAsync succeeding does NOT
                // mean the row was saved - check the output explicitly or failures
                // vanish silently.
                if (result.ErrorCode != 0)
                {
                    _logger.LogError(
                        "usp_ApiLogs_Save reported an error ({Action}) for {ServiceName}: {ErrorCode} {ErrorMessage}",
                        action, model.ServiceName, result.ErrorCode, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                // Logging must never take an API down. Swallow, log to Serilog
                // (which still has its console/file sink even if the DB sink fails),
                // and let the request continue.
                _logger.LogError(ex, "Failed to persist API log ({Action}) for {ServiceName}", action, model.ServiceName);
                return new ApiLogResponseModel { Id = 0, ErrorCode = -1, ErrorMessage = ex.Message };
            }
        }
    }
}
