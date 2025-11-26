# Smoke Test Script for CRS API Endpoints
# Run this script to test basic API functionality

$baseUrl = "https://www.alxreservecloud.com"  # Adjust if running on different port

Write-Host "Running smoke tests for CRS API..."

# Test billing status
Write-Host "Testing /api/billing/status/1..."
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/billing/status/1" -Method GET
    Write-Host "Status: $($response.StatusCode) - OK"
} catch {
    Write-Host "Failed: $($_.Exception.Message)"
}

# Test billing invoices
Write-Host "Testing /api/billing/invoices/1..."
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/billing/invoices/1" -Method GET
    Write-Host "Status: $($response.StatusCode) - OK"
} catch {
    Write-Host "Failed: $($_.Exception.Message)"
}

# Test tickets open
Write-Host "Testing /api/tickets/open..."
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/tickets/open" -Method GET
    Write-Host "Status: $($response.StatusCode) - OK"
} catch {
    Write-Host "Failed: $($_.Exception.Message)"
}

# Test customers count
Write-Host "Testing /api/customers/count..."
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/customers/count" -Method GET
    Write-Host "Status: $($response.StatusCode) - OK"
} catch {
    Write-Host "Failed: $($_.Exception.Message)"
}

# Test customers list
Write-Host "Testing /api/customers..."
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/customers?page=1&pageSize=10" -Method GET
    Write-Host "Status: $($response.StatusCode) - OK"
} catch {
    Write-Host "Failed: $($_.Exception.Message)"
}

Write-Host "Smoke tests completed."