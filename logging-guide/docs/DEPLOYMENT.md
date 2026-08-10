# Deployment & Update Strategy for LMKR.Shared.Logging

This document explains how LMKR.Shared.Logging is deployed, distributed, and consumed by the 20+ microservices in the LMKR ecosystem.

## Architecture Overview

```
LMKR.Shared.Logging Repository
		 ↓
   Main Branch Merge
		 ↓
   Azure Pipelines CI/CD
   ├─ Restore / Build / Test
   ├─ GitVersion calculates semantic version
   ├─ Pack NuGet package (.nupkg + .snupkg)
		 ↓
   Azure Artifacts Feed (LMKR-Shared-Packages)
		 ↓
   GitHub Dependabot Scanner
		 ↓
   Auto PR in consuming service repos
		 ↓
   Consuming Service Team Reviews & Merges
```

## Publishing Pipeline

### 1. Commit & Push to Main

```bash
# Your feature is ready. Commit with semantic convention:
git commit -m "feat: add configurable request timeout for slow APIs"
git push origin main
```

### 2. Azure Pipelines Automatically:

1. **Calculates version** via GitVersion based on your commit message
   - `fix:` → Patch bump (1.0.0 → 1.0.1)
   - `feat:` → Minor bump (1.0.0 → 1.1.0)
   - `+semver:major` → Major bump (1.0.0 → 2.0.0)

2. **Builds & tests** the package
   - Restores dependencies from LMKR-Shared-Packages feed
   - Builds in Release configuration
   - Runs unit tests (if added to repo)

3. **Creates NuGet package**
   - `LMKR.Shared.Logging.1.1.0.nupkg` (release package)
   - `LMKR.Shared.Logging.1.1.0.snupkg` (debug symbols)

4. **Publishes to Azure Artifacts**
   - Package added to `LMKR-Shared-Packages` feed
   - Available to all consuming services

### 3. Consuming Services Auto-Notified

Within hours, GitHub Dependabot will:
- Scan consuming service repositories for outdated packages
- Create a PR to update `LMKR.Shared.Logging` version in `.csproj`
- Link to release notes in this repository

## Consuming Service Setup (One-time)

If your microservice has NOT been set up to consume LMKR.Shared.Logging, follow these steps:

### Step 1: Create `nuget.config` at repo root

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
	<clear />
	<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
	<add key="LMKR-Shared-Packages" 
		 value="https://pkgs.dev.azure.com/[ORG]/[PROJECT]/_packaging/LMKR-Shared-Packages/nuget/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
	<LMKR-Shared-Packages>
	  <add key="Username" value="[PAT_USERNAME]" />
	  <add key="ClearTextPassword" value="[PAT_TOKEN]" />
	</LMKR-Shared-Packages>
  </packageSourceCredentials>
</configuration>
```

**Note:** Replace `[ORG]`, `[PROJECT]` with your Azure DevOps organization/project.

For CI/CD pipelines, use `NuGetAuthenticate@1` task (see step 3).

### Step 2: Add Package Reference to `.csproj`

```xml
<ItemGroup>
  <PackageReference Include="LMKR.Shared.Logging" Version="1.0.0" />
</ItemGroup>
```

### Step 3: Update CI/CD Pipeline (e.g., azure-pipelines.yml)

Add authentication before restore:

```yaml
- task: NuGetAuthenticate@1
  displayName: 'Authenticate to Azure Artifacts'

- task: DotNetCoreCLI@2
  displayName: 'Restore'
  inputs:
	command: 'restore'
	projects: '**/*.csproj'
```

### Step 4: Use in Startup Code

```csharp
// Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// 1. Register shared logging
builder.Services.AddSharedLogging(builder.Configuration);

// 2. Configure in appsettings.json
var app = builder.Build();
app.UseSharedLogging();

// 3. appsettings.json
{
  "SharedLogging": {
	"Environment": "Production",
	"SqlConnectionString": "Server=...;Database=SharedLogs;",
	"HttpRequestLogging": true,
	"HttpResponseLogging": true,
	"MaxEntityCountLimit": 1000,
	"SeqServerUrl": "https://seq.internal",
	"ElasticsearchUrl": "https://elasticsearch.internal",
	"ApplicationInsightsKey": "..."
  }
}
```

## Update Workflow

### For Patch/Minor Updates (Backward Compatible)

When Dependabot opens a PR with a minor or patch version bump:

1. Review the CHANGELOG.md in this repo
2. Verify no breaking changes
3. Run your service's tests locally
4. Approve and merge the PR
5. Your CI/CD automatically rebuilds with the new version

**Timeline:** Usually safe to auto-merge with a bot approval rule.

### For Major Updates (May Contain Breaking Changes)

When Dependabot opens a PR with a major version bump:

1. **Read the breaking changes section** in [CHANGELOG.md](../CHANGELOG.md)
2. **Review the code changes** in the LMKR.Shared.Logging repository
3. **Test extensively** locally:
   ```bash
   dotnet restore
   dotnet build
   dotnet test
   ```
4. **Update your code** if necessary (e.g., renamed APIs, config structure changes)
5. **Merge the PR** when ready (likely after making your own code changes)

**Timeline:** Review carefully; may require code changes in your service.

## Rollback Procedure

If a new release introduces issues and needs to be rolled back:

### For Consuming Services

1. Create a branch from `main`
2. Manually edit `.csproj` to older version:
   ```xml
   <PackageReference Include="LMKR.Shared.Logging" Version="1.0.2" />
   ```
3. Update CHANGELOG.md with rollback reason
4. Create and merge a PR
5. Push to main; your CI/CD rebuilds with the older package

### For LMKR.Shared.Logging Repository

If a release must be deleted/yanked:

1. Contact the Azure DevOps admin
2. In Azure Artifacts → LMKR-Shared-Packages feed
3. Find the problematic version
4. Right-click → "Unpublish" or "Delete"
5. Notify all consuming service teams to roll back

**Prevention:** Always test locally with `./build/pack.ps1` before pushing to main.

## Monitoring & Alerts

### Check Feed Status

https://dev.azure.com/[ORG]/[PROJECT]/_packaging/LMKR-Shared-Packages/packages

Look for:
- Latest version published
- Download counts
- Dependabot status

### Version Adoption

Run this query to see which consuming services have updated:

```bash
# In your consuming service repos, check recent main commits:
git log --grep="LMKR.Shared.Logging" --oneline | head -20
```

## Versioning Quick Reference

| Type        | Example       | Trigger                    | Update Type |
|-------------|---------------|----------------------------|-------------|
| Patch       | 1.0.0→1.0.1   | `fix:` or `+semver:patch`   | Auto-safe   |
| Minor       | 1.0.0→1.1.0   | `feat:` or `+semver:minor`  | Auto-safe   |
| Major       | 1.0.0→2.0.0   | `+semver:major`             | Review      |

## Troubleshooting

### NuGet Restore Fails with 401 Unauthorized

- Verify `nuget.config` exists at repo root
- Check PAT token in `nuget.config` hasn't expired
- For CI/CD: Ensure `NuGetAuthenticate@1` task runs before restore

### Consumer Service Uses Outdated Version

- Dependabot may be disabled in the repo settings
- Check GitHub → Settings → Code security & analysis → Dependabot
- Manually update `.csproj` and create a PR

### New Release Not Showing in Azure Artifacts

- Check Azure Pipeline build status (may still be running)
- Verify package name is exactly `LMKR.Shared.Logging` (case-insensitive but consistent)
- Try `dotnet nuget list source` to verify feed connectivity

## FAQ

**Q: Do I need to update my service when a new version is released?**
A: No, but recommended. Security patches (patch versions) should be applied promptly. Features (minor versions) can wait. Major versions require code review.

**Q: What if two release are published on the same day?**
A: GitVersion handles this. If you merge two PRs with `feat:` commits, you get two minor bumps: 1.0.0 → 1.1.0 → 1.2.0.

**Q: Can I use a specific commit version?**
A: Yes, but not recommended. Instead, wait for a release and reference that version. If urgent, use a local build via `./build/pack.ps1` for development only.

**Q: Who approves releases?**
A: Anyone with merge access to `main`. However, follow semantic versioning strictly so consumers can predict impact.

## References

- [Azure Artifacts Documentation](https://learn.microsoft.com/en-us/azure/devops/artifacts/get-started-nuget)
- [GitHub Dependabot for NuGet](https://github.blog/2020-06-01-github-dependabot-for-nuget/)
- [Semantic Versioning](https://semver.org/)
- [Keep a Changelog](https://keepachangelog.com/)
