#Requires -Version 5.1
<#
.SYNOPSIS
    Adds a new EF Core migration and generates an idempotent SQL script.
.DESCRIPTION
    Run from anywhere in the repo. Targets api/Skoleoverblikket.Api automatically.
    Never modifies existing migration files — only adds new ones.
.PARAMETER MigrationName
    The migration name in PascalCase, e.g. "AddSchoolTable".
.EXAMPLE
    .\scripts\add-migration.ps1 -MigrationName AddSchoolTable
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$MigrationName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot   = Split-Path $PSScriptRoot -Parent
$apiProject = Join-Path $repoRoot "api/Skoleoverblikket.Api/Skoleoverblikket.Api.csproj"
$sqlOutput  = Join-Path $repoRoot "api/Skoleoverblikket.Api/Data/Migrations/migration_script.sql"

try {
    Write-Host "Adding EF Core migration '$MigrationName'..." -ForegroundColor Cyan

    $output = & dotnet ef migrations add $MigrationName `
        --project $apiProject `
        --output-dir Data/Migrations `
        2>&1

    if ($LASTEXITCODE -ne 0) {
        $output | ForEach-Object { Write-Host $_ }
        throw "dotnet ef migrations add failed."
    }

    Write-Host "Migration added. Generating idempotent SQL script..." -ForegroundColor Cyan

    $scriptOutput = & dotnet ef migrations script `
        --project $apiProject `
        --idempotent `
        --output $sqlOutput `
        2>&1

    if ($LASTEXITCODE -ne 0) {
        $scriptOutput | ForEach-Object { Write-Host $_ }
        throw "dotnet ef migrations script failed."
    }

    Write-Host "`n[OK] Migration '$MigrationName' created." -ForegroundColor Green
    Write-Host "     SQL script: api/Skoleoverblikket.Api/Data/Migrations/migration_script.sql" -ForegroundColor Green
}
catch {
    Write-Host "`n[FAIL] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
