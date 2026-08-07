using LMKR.Shared.Logging.Models;

namespace LMKR.Shared.Logging.Repositories
{
    public interface IApiLoggingRepository
    {
        /// <summary>Inserts the request half of the log row. Calls the single
        /// shared stored procedure usp_ApiLogs_Save with @Action = 'INSERT'.</summary>
        Task<ApiLogResponseModel> LogRequestAsync(ApiLogModel model);

        /// <summary>Updates the same row with the response half. Calls
        /// usp_ApiLogs_Save with @Action = 'UPDATE'.</summary>
        Task<ApiLogResponseModel> LogResponseAsync(ApiLogModel model);
    }
}
