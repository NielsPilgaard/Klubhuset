#Requires -Version 5.1
<#
.SYNOPSIS
    Fires a Stripe test-mode event against the local API. The webhook listener itself
    runs automatically as part of `aspire run` (the stripe-listen container) — this
    script is only for manually triggering one-off events.
.DESCRIPTION
    Runs `stripe trigger <event>`, using the same Stripe test key as the running
    Aspire stack. Requires the Aspire stack to already be running.
.PARAMETER EventType
    The Stripe event type to trigger, e.g. checkout.session.completed.
    Defaults to listing available events if omitted.
#>
param(
    [string]$EventType
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step($message) { Write-Host "`n=> $message" -ForegroundColor Cyan }
function Write-Ok($message)   { Write-Host "   [OK]   $message" -ForegroundColor Green }
function Write-Fail($message) { Write-Host "   [FAIL] $message" -ForegroundColor Red }

# ── Preflight ────────────────────────────────────────────────────────────────

Write-Step "Checking Stripe CLI..."

$stripeVersion = $null
try { $stripeVersion = (stripe --version 2>$null) } catch {}

if (-not $stripeVersion) {
    Write-Fail "Stripe CLI not found. Install it:"
    Write-Host "   winget install Stripe.StripeCLI" -ForegroundColor Yellow
    exit 1
}

Write-Ok "Stripe CLI found: $stripeVersion"

if (-not $EventType) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host " Stripe Event Trigger" -ForegroundColor Magenta
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host ""
    Write-Host " The webhook listener runs automatically via 'aspire run' (stripe-listen" -ForegroundColor White
    Write-Host " container) — no separate listener terminal needed." -ForegroundColor White
    Write-Host ""
    Write-Host " Usage: .\scripts\stripe-listen.ps1 -EventType <event-type>" -ForegroundColor White
    Write-Host ""
    Write-Host " Common events:" -ForegroundColor White
    Write-Host "   checkout.session.completed" -ForegroundColor Cyan
    Write-Host "   customer.subscription.updated" -ForegroundColor Cyan
    Write-Host "   invoice.payment_failed" -ForegroundColor Cyan
    Write-Host "   invoice.payment_succeeded" -ForegroundColor Cyan
    Write-Host "   customer.subscription.deleted" -ForegroundColor Cyan
    Write-Host ""
    Write-Host " See docs/STRIPE_LOCAL.md for the full reference." -ForegroundColor DarkGray
    exit 0
}

# ── Trigger the event ────────────────────────────────────────────────────────

Write-Step "Triggering $EventType ..."
stripe trigger $EventType
