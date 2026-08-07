using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LMKR.Shared.Logging.Configuration;
using Serilog.Context;

namespace LMKR.Shared.Logging.Middleware
{
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-Id";
        private const string ClientIdHeader = "X-Client-Id"; // fixed typo from the old "ClentIdHeader"

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private readonly string _serviceName;

        public CorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<CorrelationIdMiddleware> logger,
            IOptions<SharedLoggingOptions> options)
        {
            _next = next;
            _logger = logger;
            _serviceName = options.Value.ServiceName;
        }

        public static bool IsGrpc(HttpContext ctx) =>
            ctx.Request.Protocol == "HTTP/2" &&
            ctx.Request.ContentType?.StartsWith("application/grpc") == true;

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
            var clientId = context.Request.Headers[ClientIdHeader].FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N")[..12];
                _logger.LogDebug("No CorrelationId received, generated: {CorrelationId}", correlationId);
            }

            context.Items["CorrelationId"] = correlationId;
            context.Items["ClientId"] = clientId;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);
                if (!string.IsNullOrEmpty(clientId))
                {
                    context.Response.Headers.TryAdd(ClientIdHeader, clientId);
                }
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("ClientId", clientId))
            using (LogContext.PushProperty("Service", _serviceName))
            {
                if (IsGrpc(context))
                {
                    _logger.LogInformation(
                        "gRPC call {Method} from {RemoteIp}",
                        context.Request.Path,
                        context.Connection.RemoteIpAddress);
                }

                await _next(context);
            }
        }
    }
}
