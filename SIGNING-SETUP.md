# Package Signing Setup for Fluent.TaskScheduler

This document outlines the package signing implementation for the Fluent.TaskScheduler NuGet package, following [Microsoft's NuGet package signing guidelines](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package).

## Overview

The project now supports both **assembly strong naming** and **NuGet package signing**:

- **Strong Naming**: Assemblies are signed with a strong name key for compatibility with environments requiring signed assemblies
- **Package Signing**: NuGet packages are signed with a code signing certificate for integrity verification

## Files Created/Modified

### Strong Naming
- `FluentTaskScheduler.snk` - Strong name key file (excluded from git)
- `Fluent.TaskScheduler.csproj` - Updated with signing properties

### Package Signing
- `FluentTaskScheduler.cer` - Test certificate file (excluded from git)
- `build-and-sign.ps1` - Local build and signing script
- `.github/workflows/publish-nuget.yml` - GitHub Actions workflow for NuGet.org
- `.github/workflows/publish-github-packages.yml` - GitHub Actions workflow for GitHub Packages

### Configuration
- `.gitignore` - Updated to exclude signing files (*.snk, *.cer, *.pfx)

## Local Development

### Building and Signing Locally

Use the provided PowerShell script:

```powershell
# Build and sign packages
.\build-and-sign.ps1

# Build without signing
.\build-and-sign.ps1 -SkipSigning

# Build in Debug configuration
.\build-and-sign.ps1 -Configuration Debug
```

### Certificate Management

The test certificate was created using:

```powershell
New-SelfSignedCertificate -Subject "CN=Fluent.TaskScheduler Test Developer, OU=Use for testing purposes ONLY" `
                          -FriendlyName "FluentTaskSchedulerTestDeveloper" `
                          -Type CodeSigning `
                          -KeyUsage DigitalSignature `
                          -KeyLength 2048 `
                          -KeyAlgorithm RSA `
                          -HashAlgorithm SHA256 `
                          -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
                          -CertStoreLocation "Cert:\CurrentUser\My"
```

## Production Signing

### For Production Use

1. **Obtain a Code Signing Certificate** from a trusted CA:
   - Certum
   - Comodo
   - DigiCert
   - GlobalSign
   - SSL.com

2. **Register Certificate with NuGet.org**:
   - Export certificate to `.cer` format
   - Upload to NuGet.org account settings
   - Configure signing requirements

3. **Update GitHub Secrets**:
   - `NUGET_API_KEY` - Your NuGet.org API key
   - `SIGNING_CERT_THUMBPRINT` - Production certificate thumbprint

### GitHub Actions Workflow

The workflow automatically:
1. Builds the project using the full .NET path
2. Runs tests
3. Creates NuGet packages
4. Signs packages (if certificate is configured)
5. Publishes to NuGet.org

## Project Configuration

### Strong Naming Properties

```xml
<PropertyGroup>
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>FluentTaskScheduler.snk</AssemblyOriginatorKeyFile>
  <DelaySign>false</DelaySign>
</PropertyGroup>
```

### Package Metadata

```xml
<PropertyGroup>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageReleaseNotes>See https://github.com/Cadtastic-Solutions/Fluent.TaskScheduler/releases for release notes.</PackageReleaseNotes>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

## Expected Warnings

When using test certificates, these warnings are normal:

- **NU3043**: Invalid certificate fingerprint format (SHA-1 vs SHA-256)
- **NU3018**: Untrusted root certificate (self-signed test certificate)

These warnings will not appear with production certificates from trusted CAs.

## Verification

To verify package signatures:

```powershell
# Verify a signed package
& "C:\Program Files\dotnet\dotnet.exe" nuget verify .\artifacts\Fluent.TaskScheduler.1.0.0.nupkg
```

## Security Notes

- Strong name key files (*.snk) contain private keys and must not be committed to source control
- Test certificates are for development only and should not be used in production
- Production certificates should be stored securely and access should be limited
- Certificate thumbprints can be safely stored in GitHub Secrets

## References

- [Microsoft NuGet Package Signing Documentation](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package)
- [NuGet Signed Packages Reference](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference)
- [Strong Naming Assemblies](https://learn.microsoft.com/en-us/dotnet/standard/assembly/strong-named) 