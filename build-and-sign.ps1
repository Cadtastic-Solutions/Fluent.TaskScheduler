#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build and sign the Fluent.TaskScheduler NuGet package locally
.DESCRIPTION
    This script builds the project, creates the NuGet package, and signs it with the test certificate
.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Release.
.PARAMETER SkipSigning
    Skip package signing step
#>

param(
    [string]$Configuration = "Release",
    [switch]$SkipSigning
)

$ErrorActionPreference = "Stop"

Write-Host "Building Fluent.TaskScheduler..." -ForegroundColor Green

# Clean previous builds
if (Test-Path "./artifacts") {
    Remove-Item "./artifacts" -Recurse -Force
}

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
& "C:\Program Files\dotnet\dotnet.exe" restore

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
& "C:\Program Files\dotnet\dotnet.exe" build --configuration $Configuration --no-restore

# Run tests (if any exist)
Write-Host "Running tests..." -ForegroundColor Yellow
& "C:\Program Files\dotnet\dotnet.exe" test --configuration $Configuration --no-build --verbosity normal

# Pack the project
Write-Host "Creating NuGet package..." -ForegroundColor Yellow
& "C:\Program Files\dotnet\dotnet.exe" pack --configuration $Configuration --no-build --output ./artifacts

# Sign the package (if not skipped)
if (-not $SkipSigning) {
    Write-Host "Signing packages..." -ForegroundColor Yellow
    
    # Check if certificate exists
    $cert = Get-ChildItem -Path "Cert:\CurrentUser\My" | Where-Object {$_.Subject -like "*Fluent.TaskScheduler*"} | Select-Object -First 1
    
    if ($cert) {
        Write-Host "Found certificate: $($cert.Subject)" -ForegroundColor Green
        Write-Host "Certificate thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
        
        Get-ChildItem ./artifacts/*.nupkg | ForEach-Object {
            Write-Host "Signing package: $($_.Name)" -ForegroundColor Cyan
            & "C:\Program Files\dotnet\dotnet.exe" nuget sign $_.FullName --certificate-store-name "My" --certificate-store-location "CurrentUser" --certificate-fingerprint $cert.Thumbprint --timestamper "http://timestamp.digicert.com"
        }
        
        # Verify signatures
        Write-Host "Verifying package signatures..." -ForegroundColor Yellow
        Get-ChildItem ./artifacts/*.nupkg | ForEach-Object {
            Write-Host "Verifying: $($_.Name)" -ForegroundColor Cyan
            & "C:\Program Files\dotnet\dotnet.exe" nuget verify $_.FullName
        }
    } else {
        Write-Warning "No signing certificate found. Run the following command to create one:"
        Write-Host 'New-SelfSignedCertificate -Subject "CN=Fluent.TaskScheduler Test Developer, OU=Use for testing purposes ONLY" -FriendlyName "FluentTaskSchedulerTestDeveloper" -Type CodeSigning -KeyUsage DigitalSignature -KeyLength 2048 -KeyAlgorithm RSA -HashAlgorithm SHA256 -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" -CertStoreLocation "Cert:\CurrentUser\My"' -ForegroundColor Gray
    }
} else {
    Write-Host "Skipping package signing as requested." -ForegroundColor Yellow
}

# List created packages
Write-Host "`nCreated packages:" -ForegroundColor Green
Get-ChildItem ./artifacts/*.nupkg | ForEach-Object {
    Write-Host "  $($_.Name) ($([math]::Round($_.Length / 1KB, 2)) KB)" -ForegroundColor Cyan
}

Write-Host "`nBuild completed successfully!" -ForegroundColor Green
Write-Host "Packages are available in the ./artifacts folder" -ForegroundColor Gray 