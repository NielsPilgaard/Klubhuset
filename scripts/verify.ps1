#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run all CI checks locally. Collect errors from all steps before exiting.
.PARAMETER Fix
    Auto-fix dotnet formatting violations instead of just reporting them.
.PARAMETER SkipTests
    Skip API integration tests (useful for quick compile checks).
.PARAMETER SkipFrontend
    Skip ESLint and TypeScript build steps.
.PARAMETER SkipDotnet
    Skip all dotnet steps (format, build, tests).
#>
param(
    [switch]$Fix,
    [switch]$SkipTests,
    [switch]$SkipFrontend,
    [switch]$SkipDotnet
)

$ErrorActionPreference = 'Continue'
$RepoRoot = Split-Path $PSScriptRoot -Parent
$Errors = @()
$StepResults = @()

function Step([string]$name, [scriptblock]$body) {
    Write-Host "`n==> $name" -ForegroundColor Cyan
    $output = & $body 2>&1
    $exit = $LASTEXITCODE
    if ($exit -ne 0) {
        $script:Errors += "FAIL [$name]`n$output"
        $script:StepResults += @{ Name = $name; Pass = $false }
        Write-Host $output
        Write-Host "FAIL: $name" -ForegroundColor Red
    } else {
        $script:StepResults += @{ Name = $name; Pass = $true }
        Write-Host "PASS: $name" -ForegroundColor Green
    }
}

Push-Location $RepoRoot

if (-not $SkipFrontend) {
    Step "ESLint" {
        Set-Location "$RepoRoot/web"
        npm run lint
    }

    Step "TypeScript build" {
        Set-Location "$RepoRoot/web"
        npm run build
    }
}

if (-not $SkipDotnet) {
    Step "dotnet format" {
        Set-Location $RepoRoot
        if ($Fix) {
            dotnet format api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj
        } else {
            dotnet format api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj --verify-no-changes
        }
    }

    Step "dotnet build" {
        Set-Location $RepoRoot
        dotnet build api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj --configuration Release -p:CI=true
    }

    if (-not $SkipTests) {
        Step "API integration tests" {
            Set-Location $RepoRoot
            dotnet test --project api/tests/Skoleoverblikket.Api.IntegrationTests/Skoleoverblikket.Api.IntegrationTests.csproj --configuration Release
        }
    }
}

Pop-Location

Write-Host "`n==> Summary" -ForegroundColor Cyan
foreach ($r in $StepResults) {
    $color = if ($r.Pass) { 'Green' } else { 'Red' }
    $icon  = if ($r.Pass) { 'PASS' } else { 'FAIL' }
    Write-Host "  $icon  $($r.Name)" -ForegroundColor $color
}

if ($Errors.Count -gt 0) {
    Write-Host "`n$($Errors.Count) step(s) failed:" -ForegroundColor Red
    $Errors | ForEach-Object { Write-Host "`n$_" -ForegroundColor Red }
    exit 1
}

Write-Host "`nAll checks passed." -ForegroundColor Green
exit 0
