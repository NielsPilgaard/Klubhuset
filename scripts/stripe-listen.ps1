#Requires -Version 5.1
<#
.SYNOPSIS
    Starts the Stripe CLI webhook listener and forwards events to the local API.
.DESCRIPTION
    Runs `stripe listen` forwarding to http://localhost:5000/api/v1/stripe/webhook.
    Prints the webhook signing secret and tells you where to put it.
    Requires: Stripe CLI installed and logged in (`stripe login`).
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step($message) { Write-Host "`n=> $message" -ForegroundColor Cyan }
function Write-Ok($message)   { Write-Host "   [OK]   $message" -ForegroundColor Green }
function Write-Fail($message) { Write-Host "   [FAIL] $message" -ForegroundColor Red }
function Write-Info($message) { Write-Host "   [INFO] $message" -ForegroundColor Yellow }

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

# ── Instructions ─────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host " Stripe Webhook Listener" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""
Write-Host " The CLI will print a webhook signing secret on startup:" -ForegroundColor White
Write-Host ""
Write-Host "   Your webhook signing secret is whsec_xxxx..." -ForegroundColor DarkGray
Write-Host ""
Write-Host " Copy that secret into:" -ForegroundColor White
Write-Host "   api/Skoleoverblikket.Api/appsettings.Development.json" -ForegroundColor Yellow
Write-Host "   -> Stripe:WebhookSecret" -ForegroundColor Yellow
Write-Host ""
Write-Host " Then leave this terminal open and use a second terminal to trigger events:" -ForegroundColor White
Write-Host "   stripe trigger checkout.session.completed" -ForegroundColor Cyan
Write-Host "   stripe trigger customer.subscription.updated" -ForegroundColor Cyan
Write-Host "   stripe trigger invoice.payment_failed" -ForegroundColor Cyan
Write-Host "   stripe trigger customer.subscription.deleted" -ForegroundColor Cyan
Write-Host ""
Write-Host " See docs/STRIPE_LOCAL.md for the full event list." -ForegroundColor DarkGray
Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host " Starting listener — press Ctrl+C to stop" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

# ── Start listener ───────────────────────────────────────────────────────────

stripe listen --forward-to http://localhost:5000/api/v1/stripe/webhook
