using Grpc.Core;
using Grpc.Core.Interceptors;
using Serilog.Context;

public class CorrelationIdInterceptor : Interceptor
{
    private const string CorrelationIdHeader = "x-correlation-id";
    private const string ClientIdHeader = "x-client-id";

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var correlationId = context.RequestHeaders
            .FirstOrDefault(h => h.Key == CorrelationIdHeader)?.Value;

        var clientId = context.RequestHeaders
            .FirstOrDefault(h => h.Key == ClientIdHeader)?.Value;

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N")[..12];
        }

        context.ResponseTrailers.Add(CorrelationIdHeader, correlationId);

        if (!string.IsNullOrWhiteSpace(clientId))
        {
            context.ResponseTrailers.Add(ClientIdHeader, clientId);
        }

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("ClientId", clientId ?? string.Empty))
        using (LogContext.PushProperty("Service", "PremiumApi"))
        {
            try
            {
                return await continuation(request, context);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}