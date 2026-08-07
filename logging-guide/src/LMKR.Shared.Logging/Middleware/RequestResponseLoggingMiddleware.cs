using System.Diagnostics;
using System.Text;
using LMKR.Shared.Logging.Configuration;
using LMKR.Shared.Logging.Models;
using LMKR.Shared.Logging.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace LMKR.Shared.Logging.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private static readonly RecyclableMemoryStreamManager StreamManager = new();

        private readonly RequestDelegate _next;
        private readonly IApiLoggingRepository _repo;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly SharedLoggingOptions _options;

        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            IApiLoggingRepository repo,
            ILogger<RequestResponseLoggingMiddleware> logger,
            IOptions<SharedLoggingOptions> options)
        {
            _next = next;
            _repo = repo;
            _logger = logger;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // gRPC bodies are protobuf-framed and not useful as logged text -
            // pass straight through, correlation/trace logging already
            // happened in CorrelationIdMiddleware.
            if (CorrelationIdMiddleware.IsGrpc(context))
            {
                await _next(context);
                return;
            }

            if (!_options.HttpRequestLogging && !_options.HttpResponseLogging)
            {
                await _next(context);
                return;
            }

            var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;
            var clientId = context.Items["ClientId"]?.ToString();
            var model = new ApiLogModel
            {
                ServiceName = _options.ServiceName,
                CorrelationId = correlationId,
                ClientId = clientId,
                APIMethod = context.Request.Method,
                APIURL = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}",
                ClientIP = ResolveClientIp(context),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                CreatedBy = 0
            };

            var stopwatch = Stopwatch.StartNew();

            // A row must exist before it can be updated with the response, so
            // we insert whenever EITHER flag is on. HttpRequestLogging just
            // controls whether the request body is captured on that insert;
            // HttpResponseLogging alone (request body capture off) still
            // produces a row, it just starts empty and gets the response
            // fields filled in below.
            if (_options.HttpRequestLogging || _options.HttpResponseLogging)
            {
                try
                {
                    model.RequestBody = (_options.HttpRequestLogging && _options.LogBody)
                        ? await ReadRequestBodyAsync(context)
                        : null;
                    var response = await _repo.LogRequestAsync(model);
                    model.Id = response.Id;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to log request for {Path}", context.Request.Path);
                }
            }

            if (!_options.HttpResponseLogging)
            {
                await _next(context);
                return;
            }

            var originalBodyStream = context.Response.Body;
            await using var responseBodyStream = StreamManager.GetStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBodyStream).ReadToEndAsync();
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;

                if (model.Id != 0)
                {
                    model.ResponseBody = _options.LogBody ? Truncate(responseText, _options.MaxBodyLength) : null;
                    model.StatusCode = context.Response.StatusCode;
                    model.DurationMs = stopwatch.ElapsedMilliseconds;
                    model.UpdatedBy = 0;

                    try
                    {
                        await _repo.LogResponseAsync(model);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log response for {Path}", context.Request.Path);
                    }
                }
            }
        }

        private async Task<string> ReadRequestBodyAsync(HttpContext context)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            return Truncate(body, _options.MaxBodyLength);
        }

        private static string Truncate(string value, int maxLength) =>
            string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength] + "...(truncated)";

        private static string ResolveClientIp(HttpContext context)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return !string.IsNullOrEmpty(forwarded)
                ? forwarded.Split(',')[0].Trim()
                : context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Unknown";
        }
    }
}
