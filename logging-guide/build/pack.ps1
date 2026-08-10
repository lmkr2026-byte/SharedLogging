# LMKR.Shared.Logging Local Build & Pack Script
# 
# Usage:
#   ./build/pack.ps1 -Configuration Release -OutputPath ./bin/packages
#   ./build/pack.ps1 -LocalVersion "1.2.3-dev"
#
# This script packages the library locally without requiring Azure Pipelines.
# For CI/CD, the azure-pipelines.yml handles versioning and feed publication.

param(
	[string]$Configuration = "Release",
	[string]$OutputPath = "./bin/packages",
	[string]$LocalVersion = $null,
	[switch]$Push = $false,
	[string]$Feed = "LMKR-Shared-Packages"
)

$ErrorActionPreference = "Stop"

# Determine the root directory (parent of build folder)
$RootDir = Split-Path -Parent $PSScriptRoot
$ProjectPath = "$RootDir/src/LMKR.Shared.Logging/LMKR.Shared.Logging.csproj"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "LMKR.Shared.Logging - Local Pack Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Validate project exists
if (-not (Test-Path $ProjectPath)) {
	Write-Host "ERROR: Project file not found at $ProjectPath" -ForegroundColor Red
	exit 1
}

Write-Host "[1/5] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $ProjectPath
if ($LASTEXITCODE -ne 0) {
	Write-Host "ERROR: Restore failed" -ForegroundColor Red
	exit 1
}

Write-Host "[2/5] Building project ($Configuration)..." -ForegroundColor Yellow
dotnet build $ProjectPath --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
	Write-Host "ERROR: Build failed" -ForegroundColor Red
	exit 1
}

Write-Host "[3/5] Running tests..." -ForegroundColor Yellow
# Note: Add test projects here if they exist
# dotnet test path/to/tests.csproj --configuration $Configuration --no-build

Write-Host "[4/5] Packing NuGet package..." -ForegroundColor Yellow

# Build pack command
$PackArgs = @(
	"pack",
	$ProjectPath,
	"--configuration", $Configuration,
	"--output", $OutputPath,
	"--no-build"
)

if ($LocalVersion) {
	$PackArgs += "--version-suffix", $LocalVersion
	Write-Host "Using local version suffix: $LocalVersion" -ForegroundColor Cyan
}

dotnet @PackArgs
if ($LASTEXITCODE -ne 0) {
	Write-Host "ERROR: Pack failed" -ForegroundColor Red
	exit 1
}

Write-Host ""
Write-Host "[5/5] Package created successfully!" -ForegroundColor Green
$packageFile = Get-ChildItem "$OutputPath/*.nupkg" -Exclude "*.symbols.nupkg" | Select-Object -First 1
if ($packageFile) {
	Write-Host "  Location: $($packageFile.FullName)" -ForegroundColor Cyan
	Write-Host "  Size: $([Math]::Round($packageFile.Length / 1KB, 2)) KB" -ForegroundColor Cyan
	Write-Host ""
	Write-Host "To publish locally for testing, run:" -ForegroundColor Gray
	Write-Host "  dotnet nuget add source <local-feed-path> --name local-dev" -ForegroundColor Gray
	Write-Host "  dotnet nuget push $($packageFile.FullName) --source local-dev" -ForegroundColor Gray
}

if ($Push) {
	Write-Host ""
	Write-Host "[+1/5] Pushing to internal feed ($Feed)..." -ForegroundColor Yellow
	Write-Host "Note: This requires NuGetAuthenticate@1 to be configured in your CI/CD pipeline." -ForegroundColor Gray
	Write-Host "For local testing, use a local NuGet source instead." -ForegroundColor Gray
}
