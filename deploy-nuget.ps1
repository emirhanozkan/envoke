# PowerShell script for deploying Envoke package to NuGet
# This script builds, packages, and publishes the NuGet package

param(
    [string]$ApiKey = "",
    [string]$Source = "https://api.nuget.org/v3/index.json",
    [string]$Configuration = "Release",
    [string]$Version = "",
    [switch]$SkipVersionUpdate
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Envoke NuGet Deployment Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get the project directory
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $projectRoot "Envoke.csproj"

if (-not (Test-Path $projectFile)) {
    Write-Host "Error: Envoke.csproj not found at $projectFile" -ForegroundColor Red
    exit 1
}

# Check if dotnet CLI is available
try {
    $dotnetVersion = dotnet --version
    Write-Host "Using .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "Error: dotnet CLI not found. Please install .NET SDK." -ForegroundColor Red
    exit 1
}

# Step 0: Version Management
if (-not $SkipVersionUpdate) {
    Write-Host ""
    Write-Host "Step 0: Version Management" -ForegroundColor Yellow
    
    # Read current version from .csproj
    $csprojContent = Get-Content $projectFile -Raw
    if ($csprojContent -match '<Version>([\d\.]+)</Version>') {
        $currentVersion = $matches[1]
        Write-Host "Current version: $currentVersion" -ForegroundColor Cyan
        
        if ([string]::IsNullOrWhiteSpace($Version)) {
            Write-Host ""
            Write-Host "Version update options:" -ForegroundColor Yellow
            Write-Host "  1. Patch (1.0.1 -> 1.0.2)" -ForegroundColor Gray
            Write-Host "  2. Minor (1.0.1 -> 1.1.0)" -ForegroundColor Gray
            Write-Host "  3. Major (1.0.1 -> 2.0.0)" -ForegroundColor Gray
            Write-Host "  4. Custom version" -ForegroundColor Gray
            Write-Host "  5. Keep current version" -ForegroundColor Gray
            Write-Host ""
            $choice = Read-Host "Select option (1-5)"
            
            $versionParts = $currentVersion -split '\.'
            $major = [int]$versionParts[0]
            $minor = if ($versionParts.Length -gt 1) { [int]$versionParts[1] } else { 0 }
            $patch = if ($versionParts.Length -gt 2) { [int]$versionParts[2] } else { 0 }
            
            switch ($choice) {
                "1" {
                    $patch++
                    $Version = "$major.$minor.$patch"
                }
                "2" {
                    $minor++
                    $patch = 0
                    $Version = "$major.$minor.$patch"
                }
                "3" {
                    $major++
                    $minor = 0
                    $patch = 0
                    $Version = "$major.$minor.$patch"
                }
                "4" {
                    $Version = Read-Host "Enter custom version (e.g., 1.2.3)"
                    if (-not ($Version -match '^\d+\.\d+\.\d+$')) {
                        Write-Host "Error: Invalid version format. Expected format: X.Y.Z (e.g., 1.2.3)" -ForegroundColor Red
                        exit 1
                    }
                }
                "5" {
                    $Version = $currentVersion
                    Write-Host "Keeping current version: $Version" -ForegroundColor Green
                }
                default {
                    Write-Host "Invalid choice. Keeping current version." -ForegroundColor Yellow
                    $Version = $currentVersion
                }
            }
        }
        
        if ($Version -ne $currentVersion) {
            Write-Host ""
            Write-Host "Updating version from $currentVersion to $Version..." -ForegroundColor Yellow
            
            # Update version in .csproj
            $updatedContent = $csprojContent -replace '<Version>([\d\.]+)</Version>', "<Version>$Version</Version>"
            Set-Content -Path $projectFile -Value $updatedContent -NoNewline
            
            Write-Host "Version updated successfully!" -ForegroundColor Green
        } else {
            Write-Host "Version unchanged: $Version" -ForegroundColor Green
        }
    } else {
        Write-Host "Warning: Could not find Version tag in .csproj file" -ForegroundColor Yellow
        if ([string]::IsNullOrWhiteSpace($Version)) {
            $Version = Read-Host "Enter version number (e.g., 1.0.1)"
        }
    }
} else {
    Write-Host ""
    Write-Host "Skipping version update (--SkipVersionUpdate flag used)" -ForegroundColor Yellow
}

# Step 1: Clean previous builds
Write-Host ""
Write-Host "Step 1: Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean $projectFile -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Clean failed, but continuing..." -ForegroundColor Yellow
}

# Step 2: Restore packages
Write-Host ""
Write-Host "Step 2: Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $projectFile
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Package restore failed" -ForegroundColor Red
    exit 1
}

# Step 3: Build the project
Write-Host ""
Write-Host "Step 3: Building project in $Configuration configuration..." -ForegroundColor Yellow
dotnet build $projectFile -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "Build completed successfully!" -ForegroundColor Green

# Step 4: Create NuGet package
Write-Host ""
Write-Host "Step 4: Creating NuGet package..." -ForegroundColor Yellow
dotnet pack $projectFile -c $Configuration --no-build --no-restore -o (Join-Path $projectRoot "bin\$Configuration")
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Package creation failed" -ForegroundColor Red
    exit 1
}
Write-Host "Package created successfully!" -ForegroundColor Green

# Find the created package
$packagePath = Get-ChildItem -Path (Join-Path $projectRoot "bin\$Configuration") -Filter "*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $packagePath) {
    Write-Host "Error: NuGet package not found in bin\$Configuration" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Package location: $($packagePath.FullName)" -ForegroundColor Cyan
Write-Host "Package size: $([math]::Round($packagePath.Length / 1KB, 2)) KB" -ForegroundColor Cyan

# Step 5: Get API key if not provided
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Host ""
    Write-Host "Step 5: NuGet API Key required for publishing" -ForegroundColor Yellow
    Write-Host "You can get your API key from: https://www.nuget.org/account/apikeys" -ForegroundColor Gray
    $secureApiKey = Read-Host "Enter your NuGet API key" -AsSecureString
    $ApiKey = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureApiKey))
    
    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        Write-Host "Error: API key is required to publish the package" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host ""
    Write-Host "Step 5: Using provided API key" -ForegroundColor Yellow
}

# Step 6: Publish to NuGet
Write-Host ""
Write-Host "Step 6: Publishing package to NuGet..." -ForegroundColor Yellow
Write-Host "Source: $Source" -ForegroundColor Gray

# Confirm before publishing
Write-Host ""
$confirmation = Read-Host "Do you want to publish $($packagePath.Name) to NuGet? (Y/N)"
if ($confirmation -ne "Y" -and $confirmation -ne "y") {
    Write-Host "Publishing cancelled by user" -ForegroundColor Yellow
    exit 0
}

dotnet nuget push $packagePath.FullName --api-key $ApiKey --source $Source --skip-duplicate
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Error: Publishing failed" -ForegroundColor Red
    Write-Host "Please check:" -ForegroundColor Yellow
    Write-Host "  - Your API key is correct" -ForegroundColor Yellow
    Write-Host "  - The package version is not already published" -ForegroundColor Yellow
    Write-Host "  - You have permission to publish this package" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Package published successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Package: $($packagePath.Name)" -ForegroundColor Cyan
if (-not [string]::IsNullOrWhiteSpace($Version)) {
    Write-Host "Version: $Version" -ForegroundColor Cyan
}
Write-Host "You can view it at: https://www.nuget.org/packages/Envoke" -ForegroundColor Cyan
Write-Host ""

