# LMKR.Shared.Logging - Enterprise Package Overview

**LMKR.Shared.Logging** is an enterprise-grade .NET 8 class library providing centralized, production-ready logging infrastructure for all LMKR microservices.

## Quick Links

- **GitHub Repository:** https://github.com/lmkr2026-byte/SharedLogging
- **Azure Artifacts Feed:** LMKR-Shared-Packages
- **Current Version:** See [CHANGELOG.md](../CHANGELOG.md)
- **Issues & Feature Requests:** [GitHub Issues](https://github.com/lmkr2026-byte/SharedLogging/issues)

## What's Included

This package provides everything your microservice needs for enterprise logging:

### Core Features

| Feature | Purpose | Status |
|---------|---------|--------|
| **Serilog Integration** | Structured, asynchronous logging with multiple sinks (Console, File, Elasticsearch, Seq, Application Insights) | ✅ Production |
| **Request/Response Logging** | Automatic capture of incoming HTTP requests and outgoing responses with configurable body limits | ✅ Production |
| **Correlation ID Propagation** | Distributed tracing via correlation IDs across the entire request lifecycle (HTTP → gRPC) | ✅ Production |
| **Centralized SQL Storage** | Optional centralized API logging repository for audit trails and compliance | ✅ Production |
| **Exception Handling Middleware** | Structured exception logging with custom exception types (BadRequest, NotFound, Validation, etc.) | ✅ Production |
| **gRPC Logging** | Correlation ID and request logging for gRPC services | ✅ Production |
| **DisableApiLogging Attribute** | Fine-grained control: opt-out of logging for specific endpoints (e.g., health checks) | ✅ Production |
| **Configuration** | Centralized `SharedLoggingOptions` in `appsettings.json` for flexible setup | ✅ Production |

### Included Serilog Sinks

| Sink | When to Use | Configuration |
|------|------------|---------------|
| **Console** | Development + local debugging | Always included |
| **File** | Local/on-premises deployments | `Serilog:WriteTo` in config |
| **Elasticsearch** | Large-scale log aggregation | ELK stack, requires Elasticsearch URL |
| **Seq** | Structured log exploration UI | Seq on-premises or cloud, requires URL |
| **Application Insights** | Azure cloud deployments | Azure AppInsights key |

## Getting Started (5 minutes)

### 1. Add Package Reference

Edit your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="LMKR.Shared.Logging" Version="1.0.0" />
</ItemGroup>
```

### 2. Configure `appsettings.json`

```json
{
  "SharedLogging": {
	"Environment": "Production",
	"SqlConnectionString": "Server=logs-db;Database=SharedLogs;...",
	"HttpRequestLogging": true,
	"HttpResponseLogging": true,
	"MaxEntityCountLimit": 1000,
	"SeqServerUrl": "https://seq.internal",
	"ElasticsearchUrl": "https://elasticsearch.internal:9200",
	"ApplicationInsightsKey": ""
  },
  "Serilog": {
	"MinimumLevel": "Information",
	"WriteTo": [
	  { "Name": "Console" },
	  { "Name": "File", "Args": { "path": "logs/app-.txt", "rollingInterval": "Day" } }
	]
  }
}
```

### 3. Update `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add shared logging services
builder.Services.AddSharedLogging(builder.Configuration);

// Configure Serilog
builder.Host.UseSerilog();

var app = builder.Build();

// Add shared logging middleware
app.UseSharedLogging();

app.Run();
```

### 4. That's it! Start logging:

```csharp
Log.Information("Service started in {Environment}", env);
Log.Warning("Slower than expected operation took {Duration}ms", stopwatch.ElapsedMilliseconds);
Log.Error(ex, "Operation failed for resource {ResourceId}", resourceId);
```

## Architecture

### Request Flow with Distributed Tracing

```
Incoming HTTP Request
		↓
CorrelationIdMiddleware (extracts/generates correlation ID)
		↓
RequestResponseLoggingMiddleware (captures request body & metadata)
		↓
Your Business Logic (uses Log.Information, Log.Error, etc.)
		↓
RequestResponseLoggingMiddleware (captures response body & status)
		↓
Writes to all Serilog sinks:
├─ Console (development)
├─ File (local backup)
├─ Elasticsearch (centralized aggregation)
├─ Seq (exploration UI)
└─ Application Insights (Azure monitoring)
```

### Multi-Service Tracing

```
Service A (correlation-id: abc-123)
	↓
	└─> Calls Service B (correlation-id: abc-123)  ✓ Propagated
			↓
			└─> Calls Service C (correlation-id: abc-123)  ✓ Propagated
					↓
					└─> Calls gRPC Service D (metadata: correlation-id: abc-123)
							↓
							All logs aggregated by correlation-id in Elasticsearch
```

## Customization

### Disable Logging for Specific Endpoints

Use the `[DisableApiLogging]` attribute on controllers/actions:

```csharp
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
	[DisableApiLogging]  // Don't log health check spam
	[HttpGet("status")]
	public IActionResult GetStatus()
	{
		return Ok(new { status = "healthy" });
	}
}
```

### Custom Configuration

Override specific options in `appsettings.json`:

```json
{
  "SharedLogging": {
	"HttpRequestLogging": true,        // Log all incoming requests
	"HttpResponseLogging": false,      // Don't log response bodies (security)
	"MaxEntityCountLimit": 5000,       // Store up to 5000 request logs
	"Environment": "Staging"
  }
}
```

### Structured Logging Examples

```csharp
// Good: Structured logging with properties
Log.Information("User {UserId} logged in from {IpAddress}", userId, ipAddress);

// Search in Elasticsearch/Seq
// Queries like: UserId = 42 or IpAddress = "192.168.1.1"

// Good: Log objects as structured data
var requestMetadata = new { userId, timestamp, endpoint, method };
Log.Information("Request completed: {@Metadata}", requestMetadata);

// Less good: String interpolation
Log.Error($"Error processing user {userId}");  // Not searchable by UserId field
```

## Configuration Reference

### SharedLoggingOptions

```csharp
{
  "SharedLogging": {
	// Deployment environment (Development, Staging, Production, etc.)
	"Environment": "Production",

	// SQL Server connection for centralized logging storage
	"SqlConnectionString": "Server=...;Database=SharedLogs;...",

	// Enable capture of incoming HTTP request bodies (max configured in MaxEntityCountLimit)
	"HttpRequestLogging": true,

	// Enable capture of outgoing HTTP response bodies
	"HttpResponseLogging": true,

	// Maximum request/response entities to store (prevents memory bloat)
	"MaxEntityCountLimit": 1000,

	// Optional: Seq centralized log exploration UI
	"SeqServerUrl": "https://seq.internal",

	// Optional: Elasticsearch for large-scale log aggregation
	"ElasticsearchUrl": "https://elasticsearch.internal:9200",

	// Optional: Azure Application Insights
	"ApplicationInsightsKey": ""
  }
}
```

### Serilog Configuration

See `appsettings.json` example above. Key sinks:

- **Console:** Development debugging
- **File:** Local backup with daily rolling logs
- **Elasticsearch:** Production search/analysis
- **Seq:** Structured log UI exploration
- **ApplicationInsights:** Azure cloud native monitoring

## Version Management & Updates

### Automatic Updates via Dependabot

When a new version is released:

1. Dependabot creates a pull request updating your `.csproj`
2. Our GitHub Actions workflow **auto-approves & merges** patch/minor updates
3. Your service's CI/CD rebuilds with the new version
4. Major version updates require manual review (may have breaking changes)

### Semantic Versioning

| Version | Type | Impact | Action |
|---------|------|--------|--------|
| 1.0.0 → 1.0.1 | Patch | Bug fixes only | Auto-merge safe |
| 1.0.0 → 1.1.0 | Minor | New features, backward compatible | Auto-merge safe |
| 1.0.0 → 2.0.0 | Major | Potential breaking changes | Manual review required |

See [CHANGELOG.md](../CHANGELOG.md) for all releases and migration guides.

## Troubleshooting

### Logs not appearing in Elasticsearch

1. Verify `ElasticsearchUrl` is correct and reachable
2. Check firewall/network access to Elasticsearch host
3. Verify Elasticsearch credentials in `appsettings.json`
4. Check Serilog console output for connection errors

### Request/Response bodies not logged

1. Verify `"HttpRequestLogging": true` in `SharedLogging` config section
2. Check if endpoint has `[DisableApiLogging]` attribute
3. Verify `MaxEntityCountLimit` isn't too small
4. Check logs for any filtering rules

### Correlation IDs not propagating

1. Ensure `UseSharedLogging()` is called early in Startup (after routing, before auth)
2. Verify correlation ID header is being sent from upstream service (`X-Correlation-ID` by default)
3. For gRPC services: ensure `GrpcLoggingInterceptor` is registered

### Out of Memory errors

- Reduce `MaxEntityCountLimit` in config
- Disable `HttpResponseLogging` if response bodies are large
- Check for memory leaks in custom logging code

More details in [docs/DEPLOYMENT.md](./DEPLOYMENT.md#troubleshooting).

## Contributing & Reporting Issues

- **Found a bug?** [Create an issue](https://github.com/lmkr2026-byte/SharedLogging/issues)
- **Have a feature request?** [Start a discussion](https://github.com/lmkr2026-byte/SharedLogging/discussions)
- **Want to contribute code?** See [docs/CONTRIBUTING.md](./CONTRIBUTING.md)

## Support & SLA

This package supports all .NET 8 microservices in the LMKR ecosystem.

- **Critical bugs:** Hotfix released within 24 hours
- **Features:** Planned releases every 2 weeks (minor versions)
- **Security updates:** Released immediately

For urgent issues, contact the Platform Engineering team.

## Roadmap

*Planned for future releases*

- [ ] OpenTelemetry integration for distributed tracing
- [ ] Structured health check endpoint for log sink status
- [ ] Per-tenant log isolation (multi-tenant scenarios)
- [ ] Log retention policies and archive strategies
- [ ] Real-time alerting rules in Elasticsearch/Seq

## FAQ

**Q: Can I use this package outside LMKR?**
A: This package is designed for internal LMKR use. However, the code is open-source and can be forked.

**Q: What happens if a log sink fails?**
A: Serilog has built-in resilience—if one sink fails, others continue operating. Check logs for error messages.

**Q: How do I audit who changed my logging configuration?**
A: Review `appsettings.json` changes in your git history. For centralized logging, check the SQL database audit trails.

**Q: Can I log Personally Identifiable Information (PII)?**
A: Technically yes, but **avoid it**. Follow GDPR/privacy compliance. Use correlation IDs instead of usernames when possible.

**Q: Is there a performance impact?**
A: Minimal. Serilog is async and buffered. Structured logging (vs. string interpolation) is actually more efficient.

## License

MIT License - See LICENSE file in repository.

## Related Documentation

- [Getting Started Guide](./DEPLOYMENT.md) - Step-by-step consumer setup
- [Version Management & Updates](./DEPLOYMENT.md#update-workflow) - Semantic versioning strategy
- [Contributing Guide](./CONTRIBUTING.md) - How to develop and release changes
- [Release Notes](../CHANGELOG.md) - All versions and breaking changes
