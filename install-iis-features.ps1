# IIS Feature Installation Script for BossHuntingSystem
# Run this script as Administrator

Write-Host "🔧 Installing IIS Features for BossHuntingSystem..." -ForegroundColor Green

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Function to enable a feature with error handling
function Enable-Feature {
    param(
        [string]$FeatureName,
        [string]$Description
    )
    
    Write-Host "📦 Installing $Description ($FeatureName)..." -ForegroundColor Yellow
    
    try {
        $result = Enable-WindowsOptionalFeature -Online -FeatureName $FeatureName -All -NoRestart
        if ($result.RestartNeeded) {
            Write-Host "⚠️ Restart required after installation" -ForegroundColor Yellow
        }
        Write-Host "✅ $Description installed successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "❌ Failed to install $Description`: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Enable features in dependency order
$features = @(
    @{ Name = "IIS-WebServerRole"; Description = "IIS Web Server Role" },
    @{ Name = "IIS-WebServer"; Description = "IIS Web Server" },
    @{ Name = "IIS-CommonHttpFeatures"; Description = "IIS Common HTTP Features" },
    @{ Name = "IIS-HttpErrors"; Description = "IIS HTTP Errors" },
    @{ Name = "IIS-HttpLogging"; Description = "IIS HTTP Logging" },
    @{ Name = "IIS-RequestFiltering"; Description = "IIS Request Filtering" },
    @{ Name = "IIS-StaticContent"; Description = "IIS Static Content" },
    @{ Name = "IIS-DefaultDocument"; Description = "IIS Default Document" },
    @{ Name = "IIS-DirectoryBrowsing"; Description = "IIS Directory Browsing" },
    @{ Name = "IIS-WebSockets"; Description = "IIS Web Sockets" },
    @{ Name = "IIS-ASPNET45"; Description = "IIS ASP.NET 4.5" }
)

$successCount = 0
$totalFeatures = $features.Count

foreach ($feature in $features) {
    if (Enable-Feature -FeatureName $feature.Name -Description $feature.Description) {
        $successCount++
    }
    Start-Sleep -Seconds 2  # Small delay between installations
}

Write-Host "`n📊 Installation Summary:" -ForegroundColor Cyan
Write-Host "   Successfully installed: $successCount/$totalFeatures features" -ForegroundColor White

if ($successCount -eq $totalFeatures) {
    Write-Host "`n🎉 All IIS features installed successfully!" -ForegroundColor Green
    Write-Host "📋 Next steps:" -ForegroundColor Yellow
    Write-Host "   1. Install ASP.NET Core Hosting Bundle from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
    Write-Host "   2. Install Node.js from: https://nodejs.org/" -ForegroundColor White
    Write-Host "   3. Run the deployment script: .\deploy-windows-server.ps1" -ForegroundColor White
} else {
    Write-Host "`n⚠️ Some features failed to install. Please check the errors above." -ForegroundColor Yellow
    Write-Host "You may need to restart the system and try again." -ForegroundColor Yellow
}

Write-Host "`n💡 Note: If you see 'Restart required' messages, restart your system before proceeding." -ForegroundColor Cyan
