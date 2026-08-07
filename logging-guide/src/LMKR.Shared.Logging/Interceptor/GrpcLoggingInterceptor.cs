using Grpc.Core;
using Grpc.Core.Interceptors;
using LMKR.Shared.Logging.Models;
using LMKR.Shared.Logging.Repositories;
using System.Text.Json;

public class GrpcLoggingInterceptor : Interceptor
{
    private readonly IApiLoggingRepository _loggingRepo;

    public GrpcLoggingInterceptor(IApiLoggingRepository loggingRepo)
    {
        _loggingRepo = loggingRepo;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var _model = new ApiLogModel
        {
            APIMethod = "GRPC",
            APIURL = context.Method,
            RequestBody = JsonSerializer.Serialize(request),
            CreatedBy = 0,
            UserAgent = "gRPC",
            ClientIP = context.Peer // peer info from gRPC
        };

        // Insert request row
        var responseInsert = await _loggingRepo.LogRequestAsync(_model);
        _model.Id = responseInsert.Id;

        TResponse response;
        try
        {
            response = await continuation(request, context);
        }
        catch
        {
            throw;
        }

        // Update with actual response
        try
        {
            _model.ResponseBody = JsonSerializer.Serialize(response);
            _model.UpdatedBy = 0;
            await _loggingRepo.LogResponseAsync(_model);
        }
        catch
        {
            // fail-safe
        }

        return response;
    }
}