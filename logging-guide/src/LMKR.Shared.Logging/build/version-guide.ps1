# LMKR.Shared.Logging - Version Bumping Guide
#
# This script provides guidance on making version bumps via conventional commits.
# GitVersion automatically calculates versions from commit history.
#
# DO NOT manually edit version numbers in LMKR.Shared.Logging.csproj.
# Instead, follow these commit message patterns:

# ===== USAGE PATTERNS =====
#
# 1. BUG FIX (Patch bump: 1.2.3 → 1.2.4)
#    - Commit message: "fix: resolve null reference in correlation middleware"
#    - Or prefix:     "+semver:patch"
#
# 2. NEW FEATURE (Minor bump: 1.2.3 → 1.3.0)
#    - Commit message: "feat: add request timeout configuration option"
#    - Or prefix:     "+semver:minor"
#
# 3. BREAKING CHANGE (Major bump: 1.2.3 → 2.0.0)
#    - Commit message first line: "feat: restructure logging config API"
#    - Footer: "BREAKING CHANGE: LoggingOptions.IsEnabled renamed to LoggingOptions.Enabled"
#    - Or prefix:     "+semver:major"

# Example commits:

# ===== CONVENTIONAL COMMITS (Auto-detected) =====

# Patch version (bug fix):
#   git commit -m "fix: handle empty correlation ID in middleware"

# Minor version (feature):
#   git commit -m "feat: add configurable request body size limit"

# Major version (breaking change):
#   git commit -m "feat: rename RequestLoggingOptions structure
#   
#   BREAKING CHANGE: The RequestLoggingOptions class is now LoggingRequestOptions
#   with different property names. Consumers must update their appsettings.json."

# ===== MANUAL SEMVER PREFIX (Explicit control) =====

# When conventional commits aren't clear, use explicit semver:
#   git commit -m "+semver:major Upgrade Serilog to 9.0 with API changes"
#   git commit -m "+semver:minor Add support for structured logging templates"
#   git commit -m "+semver:patch Update dependency vulnerable to CVE-2024-1234"

# ===== WORKFLOW =====

# 1. Make your code changes
# 2. Commit with appropriate prefix (fix:, feat:, +semver:major, etc.)
# 3. Push to main branch
# 4. Azure Pipelines auto-detects version from commit and creates new NuGet package
# 5. Package is published to LMKR-Shared-Packages feed
# 6. Consuming services update their PackageReference Version attribute

# To check what version would be calculated:
#   git fetch
#   dotnet tool run gitversion
#   # or download GitVersion CLI: choco install gitversion.portable

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "LMKR.Shared.Logging Version Guide" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Version Strategy: Semantic Versioning (MAJOR.MINOR.PATCH)" -ForegroundColor Yellow
Write-Host ""
Write-Host "GUIDELINES:" -ForegroundColor Cyan
Write-Host "  • NEVER manually edit version in .csproj" -ForegroundColor White
Write-Host "  • Use commit message prefixes: fix:, feat:, +semver:*" -ForegroundColor White
Write-Host "  • follow Conventional Commits: https://www.conventionalcommits.org/" -ForegroundColor White
Write-Host ""
Write-Host "EXAMPLES:" -ForegroundColor Cyan
Write-Host "  Patch:  git commit -m 'fix: prevent null ref exception in middleware'" -ForegroundColor Gray
Write-Host "  Minor:  git commit -m 'feat: add request body size configuration'" -ForegroundColor Gray
Write-Host "  Major:  git commit -m '+semver:major Breaking API redesign'" -ForegroundColor Gray
Write-Host ""
Write-Host "Azure Pipelines will automatically:" -ForegroundColor Yellow
Write-Host "  1. Detect the version bump from commits" -ForegroundColor White
Write-Host "  2. Build and test the package" -ForegroundColor White
Write-Host "  3. Create .nupkg with semantic version" -ForegroundColor White
Write-Host "  4. Push to LMKR-Shared-Packages feed" -ForegroundColor White
Write-Host "  5. Notify consuming services via Dependabot" -ForegroundColor White
Write-Host ""
Write-Host "For local testing, run:" -ForegroundColor Cyan
Write-Host "  ./build/pack.ps1 -Configuration Release" -ForegroundColor Gray
Write-Host ""
