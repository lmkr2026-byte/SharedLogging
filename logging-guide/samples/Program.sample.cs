// How EVERY one of the 29 services wires up the shared library.
// This is the entire integration surface - three lines beyond the
// PackageReference in the .csproj.

using LMKR.Shared.Logging.Extensions;
using Serilog;

const string ServiceName = "ParcelManagement.API"; // only line that changes per service

var builder = WebApplication.CreateBuilder(args);

// 1) Serilog reads its sinks/levels straight from appsettings - see
//    samples/appsettings.sample.json for the "Serilog" section.
//    SerilogBootstrapper (from the shared package) wires enrichment
//    consistently for every service; only ServiceName varies.
builder.Host.UseSerilog((context, _, loggerConfig) =>
    SerilogBootstrapper.Configure(loggerConfig, context.Configuration, ServiceName));

// 2) Register the shared logging services (options + repository that
//    calls the single usp_ApiLogs_Save stored procedure).
builder.Services.AddSharedLogging(builder.Configuration);

builder.Services.AddControllers();
// ... this service's other DI registrations ...

var app = builder.Build();

app.UseRouting();

// 3) One line wires up CorrelationIdMiddleware + RequestResponseLoggingMiddleware,
//    in the right order. Put it early, before auth/controllers.
app.UseSharedLogging();

app.UseAuthorization();
app.MapControllers();

app.Run();
