# Changelog

All notable changes to LMKR.Shared.Logging will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## How versions are managed

Versions are **automatically calculated** by GitVersion from your commit history. You do NOT need to manually bump version numbers in this file or the `.csproj`. Instead:

1. **Patch version bump** (1.0.0 → 1.0.1): Commit messages starting with `fix:` or prefixed with `+semver:patch`
2. **Minor version bump** (1.0.0 → 1.1.0): Commit messages starting with `feat:` or prefixed with `+semver:minor`
3. **Major version bump** (1.0.0 → 2.0.0): Commit messages with `+semver:major` prefix or containing `BREAKING CHANGE:` footer

See [build/version-guide.ps1](../build/version-guide.ps1) for detailed examples.

### Updating this Changelog

After each release, add an entry describing the changes. Use the format below, keeping it organized by change type (Added, Changed, Deprecated, Removed, Fixed, Security).

---

## [Unreleased]

### Added
- New features currently in development

### Changed
- Behavior modifications

### Deprecated
- Features planned for removal

### Removed
- Features removed in unreleased changes

### Fixed
- Bug fixes in unreleased changes

### Security
- Security-related fixes

---

## [1.0.0] - 2024-01-XX

### Added
- Serilog integration with AspNetCore middleware
- Request/Response logging middleware for capturing HTTP payloads
- Correlation ID middleware for distributed tracing across microservices
- SQL logging repository for centralized API request/response storage
- DisableApiLogging attribute for opt-out of logging on specific endpoints
- SharedLoggingOptions configuration (environment, request/response logging toggles, entity count limit)
- Exception handling middleware with custom exception types (AppException, BadRequestException, NotFoundException, ValidationException)
- Serilog sinks: Elasticsearch, Application Insights, Seq, File, Console
- gRPC logging interceptor for distributed logging in gRPC services
- Correlation ID interceptor for gRPC context propagation

### Initial Release
- First stable release of LMKR.Shared.Logging as a NuGet package
- Ready for consumption by all LMKR microservices

---

## Breaking Changes

### 1.0.0 → 2.0.0 (When applicable)

If you implement breaking changes, document them here with migration guidance:

```
- **Before:** LoggingOptions.Enabled
  **After:** LoggingOptions.IsLoggingEnabled
  **Migration:** Rename property in appsettings.json and update configuration binding code.
```

---

## Upgrade Path for Consuming Services

When LMKR.Shared.Logging receives a new release:

1. A new version is published to the `LMKR-Shared-Packages` Azure Artifacts feed
2. Dependabot automatically scans consuming service repositories
3. A pull request is created to update the PackageReference version
4. Review breaking changes in this CHANGELOG before merging
5. For major versions: Test thoroughly before merging (likely requires code changes)
6. For minor/patch: Safe to auto-merge (backward compatible)

### Example Consumer Update (Automatic via Dependabot)

```xml
<!-- Before -->
<PackageReference Include="LMKR.Shared.Logging" Version="1.0.0" />

<!-- After (Dependabot PR) -->
<PackageReference Include="LMKR.Shared.Logging" Version="1.1.0" />
```

---

## Release Checklist

Before releasing (automated by CI/CD, verify in Azure Pipelines):

- [x] All commits follow Conventional Commits format
- [x] GitVersion calculated correct semantic version
- [x] Build passed (restore, build, test)
- [x] NuGet package created with debug symbols
- [x] Package pushed to LMKR-Shared-Packages feed
- [x] This CHANGELOG.md updated with release notes

---

## References

- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Keep a Changelog](https://keepachangelog.com/)
- [GitVersion Documentation](https://gitversion.net/)
