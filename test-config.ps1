# Test script to check configuration values
Write-Host "Testing MCP Server Configuration..." -ForegroundColor Green

# Check if appsettings.json exists and is readable
$configFile = ".\appsettings.json"
if (Test-Path $configFile) {
    Write-Host "✅ appsettings.json found" -ForegroundColor Green

    # Read and display configuration
    $config = Get-Content $configFile | ConvertFrom-Json

    Write-Host "Configuration details:" -ForegroundColor Yellow
    Write-Host "- Claude API URL: $($config.Claude.ApiUrl)" -ForegroundColor Cyan
    Write-Host "- Claude API Key: $($config.Claude.ApiKey.Substring(0, 20))..." -ForegroundColor Cyan
    Write-Host "- Claude Model: $($config.Claude.Model)" -ForegroundColor Cyan
    Write-Host "- Max Tokens: $($config.Claude.MaxTokens)" -ForegroundColor Cyan

    # Check API key length
    if ($config.Claude.ApiKey.Length -gt 0) {
        Write-Host "✅ API Key is configured (length: $($config.Claude.ApiKey.Length))" -ForegroundColor Green
    } else {
        Write-Host "❌ API Key is empty!" -ForegroundColor Red
    }
} else {
    Write-Host "❌ appsettings.json not found!" -ForegroundColor Red
}

Write-Host "`nTesting API connectivity..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:8080/health" -Method GET -TimeoutSec 5
    Write-Host "✅ Health check response:" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 3) -ForegroundColor Cyan
} catch {
    Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTesting server info..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:8080/info" -Method GET -TimeoutSec 5
    Write-Host "✅ Server info response:" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 3) -ForegroundColor Cyan
} catch {
    Write-Host "❌ Server info failed: $($_.Exception.Message)" -ForegroundColor Red
}