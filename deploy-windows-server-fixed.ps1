# Windows Server Deployment Script for BossHuntingSystem
# Run this script as Administrator

param(
    [string]$PublishPath = "C:\inetpub\wwwroot\BossHuntingSystem",
    [string]$DatabaseName = "BossHuntingSystem"
)

Write-Host "🚀 Starting BossHuntingSystem Windows Server Deployment..." -ForegroundColor Green

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Check prerequisites
Write-Host "📋 Checking prerequisites..." -ForegroundColor Yellow

# Check .NET 8.0
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET Version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET 8.0 not found. Please install .NET 8.0 SDK." -ForegroundColor Red
    exit 1
}

# Check Node.js
try {
    $nodeVersion = node --version
    Write-Host "✅ Node.js Version: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Node.js not found. Please install Node.js." -ForegroundColor Red
    exit 1
}

# Check npm
try {
    $npmVersion = npm --version
    Write-Host "✅ npm Version: $npmVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ npm not found. Please install Node.js." -ForegroundColor Red
    exit 1
}

# Create publish directory
if (!(Test-Path $PublishPath)) {
    Write-Host "📁 Creating publish directory: $PublishPath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $PublishPath -Force
}

# Step 1: Build Angular Frontend
Write-Host "🔨 Building Angular frontend..." -ForegroundColor Yellow
Set-Location "bosshuntingsystem.client"

# Install npm packages
Write-Host "📦 Installing npm packages..." -ForegroundColor Yellow
npm install
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to install npm packages." -ForegroundColor Red
    exit 1
}

# Build for production
Write-Host "🏗️ Building Angular application for production..." -ForegroundColor Yellow
npm run build:prod
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to build Angular application." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Angular frontend built successfully." -ForegroundColor Green

# Verify Angular build output exists before proceeding
Write-Host "🔍 Verifying Angular build output..." -ForegroundColor Yellow
$angularOutputPath = "..\BossHuntingSystem.Server\wwwroot"
if (Test-Path $angularOutputPath) {
    $indexHtmlPath = "$angularOutputPath\index.html"
    if (Test-Path $indexHtmlPath) {
        Write-Host "✅ Angular build output verified." -ForegroundColor Green
    } else {
        Write-Host "❌ Angular build output incomplete. index.html not found." -ForegroundColor Red
        Write-Host "Please check the Angular build process." -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "❌ Angular build output not found at: $angularOutputPath" -ForegroundColor Red
    Write-Host "Please check the Angular build process." -ForegroundColor Yellow
    exit 1
}

# Step 2: Publish .NET Application
Write-Host "🔨 Publishing .NET application..." -ForegroundColor Yellow
Set-Location "..\BossHuntingSystem.Server"

# Restore packages
Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to restore NuGet packages." -ForegroundColor Red
    exit 1
}

# Publish application
Write-Host "📤 Publishing application to: $PublishPath" -ForegroundColor Yellow
dotnet publish --configuration Release --output $PublishPath --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to publish application." -ForegroundColor Red
    exit 1
}

# Copy Angular build output to wwwroot
Write-Host "📋 Copying Angular build output..." -ForegroundColor Yellow
$wwwrootPath = "$PublishPath\wwwroot"

# The Angular build output is already in the server's wwwroot folder
# (as configured in angular.json outputPath)
$serverWwwrootPath = "wwwroot"

# Check if Angular build output exists in the server's wwwroot
if (Test-Path $serverWwwrootPath) {
    # Verify that the wwwroot contains Angular build files
    $indexHtmlPath = "$serverWwwrootPath\index.html"
    if (Test-Path $indexHtmlPath) {
        Copy-Item -Path "$serverWwwrootPath\*" -Destination $wwwrootPath -Recurse -Force
        Write-Host "✅ Angular files copied from server wwwroot to deployment wwwroot." -ForegroundColor Green
    } else {
        Write-Host "❌ Angular build output incomplete. index.html not found in server wwwroot." -ForegroundColor Red
        Write-Host "Please ensure the Angular build completed successfully by running 'npm run build:prod' in the client directory." -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "❌ Angular build output not found in server wwwroot." -ForegroundColor Red
    Write-Host "Please ensure the Angular build completed successfully by running 'npm run build:prod' in the client directory." -ForegroundColor Yellow
    Write-Host "Expected location: $((Get-Location).Path)\$serverWwwrootPath" -ForegroundColor Yellow
    exit 1
}

# Step 3: Configure IIS Application Pool
Write-Host "⚙️ Configuring IIS Application Pool..." -ForegroundColor Yellow

# Create application pool if it doesn't exist
$appPoolName = "BossHuntingSystem"
try {
    Import-Module WebAdministration
    $appPool = Get-IISAppPool -Name $appPoolName -ErrorAction SilentlyContinue
    
    if (!$appPool) {
        Write-Host "📁 Creating IIS Application Pool: $appPoolName" -ForegroundColor Yellow
        New-WebAppPool -Name $appPoolName
        Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
        Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
    } else {
        Write-Host "✅ Application Pool already exists: $appPoolName" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️ Warning: Could not configure IIS Application Pool. You may need to do this manually." -ForegroundColor Yellow
}

# Step 4: Create IIS Website
Write-Host "🌐 Creating IIS Website..." -ForegroundColor Yellow

$siteName = "BossHuntingSystem"
try {
    $site = Get-Website -Name $siteName -ErrorAction SilentlyContinue
    
    if (!$site) {
        Write-Host "📁 Creating IIS Website: $siteName" -ForegroundColor Yellow
        New-Website -Name $siteName -PhysicalPath $PublishPath -ApplicationPool $appPoolName -Port 80
    } else {
        Write-Host "✅ Website already exists: $siteName" -ForegroundColor Green
        # Update physical path
        Set-ItemProperty -Path "IIS:\Sites\$siteName" -Name "physicalPath" -Value $PublishPath
    }
} catch {
    Write-Host "⚠️ Warning: Could not configure IIS Website. You may need to do this manually." -ForegroundColor Yellow
}

# Step 5: Set Permissions
Write-Host "🔐 Setting file permissions..." -ForegroundColor Yellow
try {
    $acl = Get-Acl $PublishPath
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl -Path $PublishPath -AclObject $acl
    Write-Host "✅ File permissions set successfully." -ForegroundColor Green
} catch {
    Write-Host "⚠️ Warning: Could not set file permissions. You may need to do this manually." -ForegroundColor Yellow
}

# Step 6: Create web.config for IIS
Write-Host "📄 Creating web.config for IIS..." -ForegroundColor Yellow

$webConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\BossHuntingSystem.Server.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
"@

$webConfigPath = "$PublishPath\web.config"
$webConfigContent | Out-File -FilePath $webConfigPath -Encoding UTF8
Write-Host "✅ web.config created successfully." -ForegroundColor Green

Write-Host "🎉 Deployment completed successfully!" -ForegroundColor Green
Write-Host "📋 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Create the database '$DatabaseName' in SQL Server" -ForegroundColor White
Write-Host "   2. Update the connection string in appsettings.Production.json if needed" -ForegroundColor White
Write-Host "   3. Configure your domain name in Program.cs CORS settings" -ForegroundColor White
Write-Host "   4. Test the application at http://localhost" -ForegroundColor White
Write-Host "   5. Configure SSL certificate for HTTPS" -ForegroundColor White

Set-Location ".."
