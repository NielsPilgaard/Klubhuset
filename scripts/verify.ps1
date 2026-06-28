#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run all CI checks locally. Collect errors from all steps before exiting.
.PARAMETER NoFix
    Report dotnet formatting violations instead of auto-fixing them (default is to fix).
.PARAMETER SkipTests
    Skip API integration tests (useful for quick compile checks).
.PARAMETER SkipFrontend
    Skip Biome and TypeScript build steps.
.PARAMETER SkipDotnet
    Skip all dotnet steps (format, build, tests).
#>
param(
    [switch]$NoFix,
    [switch]$SkipTests,
    [switch]$SkipFrontend,
    [switch]$SkipDotnet
)

$ErrorActionPreference = 'Continue'
$RepoRoot = Split-Path $PSScriptRoot -Parent
$Errors = @()
$StepResults = @()

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

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
    if ($NoFix) {
        Step "Biome" {
            Set-Location "$RepoRoot/web"
            npx biome check src/
        }
    } else {
        Step "Biome (auto-fix)" {
            Set-Location "$RepoRoot/web"
            npx biome check --write src/
        }
    }

    Step "TypeScript build" {
        Set-Location "$RepoRoot/web"
        npm run build
    }
}

if (-not $SkipDotnet) {
    if ($NoFix) {
        Step "dotnet format" {
            Set-Location $RepoRoot
            dotnet format api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj --verify-no-changes
        }
    } else {
        Write-Host "`n==> dotnet format (auto-fix)" -ForegroundColor Cyan
        Set-Location $RepoRoot
        dotnet format api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj > $null 2>&1
        $fmtExit = $LASTEXITCODE
        if ($fmtExit -ne 0) {
            $script:Errors += "FAIL [dotnet format]`n(output suppressed — run with -NoFix to see violations)"
            $script:StepResults += @{ Name = "dotnet format"; Pass = $false }
            Write-Host "FAIL: dotnet format" -ForegroundColor Red
        } else {
            $script:StepResults += @{ Name = "dotnet format"; Pass = $true }
            Write-Host "PASS: dotnet format" -ForegroundColor Green
        }
    }

    Step "dotnet build" {
        Set-Location $RepoRoot
        dotnet build api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj --configuration Release -p:CI=true
    }

    if (-not $SkipTests) {
        Step "API integration tests" {
            Set-Location $RepoRoot
            dotnet test --project api/tests/Skoleoverblikket.Api.IntegrationTests/Skoleoverblikket.Api.IntegrationTests.csproj --configuration Release --no-build
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
