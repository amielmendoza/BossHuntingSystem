# Comprehensive Angular Compilation Fix Script
# This script performs a complete clean and rebuild

Write-Host "🔧 Fixing Angular compilation issues..." -ForegroundColor Green

# Navigate to Angular client directory
Set-Location "bosshuntingsystem.client"

# Step 1: Clear all caches and temporary files
Write-Host "🧹 Clearing all caches..." -ForegroundColor Yellow

# Clear Angular cache
if (Test-Path ".angular") {
    Remove-Item -Recurse -Force ".angular"
    Write-Host "✅ Angular cache cleared" -ForegroundColor Green
}

# Clear TypeScript cache
if (Test-Path "node_modules\.cache") {
    Remove-Item -Recurse -Force "node_modules\.cache"
    Write-Host "✅ TypeScript cache cleared" -ForegroundColor Green
}

# Clear npm cache
npm cache clean --force
Write-Host "✅ npm cache cleared" -ForegroundColor Green

# Step 2: Remove node_modules and reinstall
Write-Host "📦 Removing node_modules and reinstalling packages..." -ForegroundColor Yellow
if (Test-Path "node_modules") {
    Remove-Item -Recurse -Force "node_modules"
    Write-Host "✅ node_modules removed" -ForegroundColor Green
}

# Reinstall packages
Write-Host "📦 Installing packages..." -ForegroundColor Yellow
npm install
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to install packages" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Packages installed successfully" -ForegroundColor Green

# Step 3: Try building
Write-Host "🏗️ Building Angular application..." -ForegroundColor Yellow
npm run build:prod

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed! Trying development build..." -ForegroundColor Red
    
    # Try development build for better error messages
    Write-Host "🔍 Trying development build for better error diagnostics..." -ForegroundColor Yellow
    npm run build
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Development build also failed!" -ForegroundColor Red
        Write-Host "💡 Try running manually:" -ForegroundColor Yellow
        Write-Host "   ng build --verbose" -ForegroundColor White
        exit 1
    }
}

Write-Host "🎉 Angular compilation fix completed!" -ForegroundColor Green
