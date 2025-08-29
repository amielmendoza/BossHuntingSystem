# Clean Build Script for Angular Project
# This script clears all build caches and rebuilds the project

Write-Host "🧹 Cleaning Angular build cache..." -ForegroundColor Yellow

# Navigate to the Angular client directory
Set-Location "bosshuntingsystem.client"

# Clear Angular cache
Write-Host "📦 Clearing Angular cache..." -ForegroundColor Yellow
if (Test-Path ".angular") {
    Remove-Item -Recurse -Force ".angular"
    Write-Host "✅ Angular cache cleared" -ForegroundColor Green
}

# Clear node_modules (optional - uncomment if needed)
# Write-Host "📦 Clearing node_modules..." -ForegroundColor Yellow
# if (Test-Path "node_modules") {
#     Remove-Item -Recurse -Force "node_modules"
#     Write-Host "✅ node_modules cleared" -ForegroundColor Green
# }

# Clear npm cache
Write-Host "📦 Clearing npm cache..." -ForegroundColor Yellow
npm cache clean --force
Write-Host "✅ npm cache cleared" -ForegroundColor Green

# Reinstall packages (if node_modules was cleared)
# npm install

# Build for production
Write-Host "🏗️ Building Angular application for production..." -ForegroundColor Yellow
npm run build:prod

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "🎉 Clean build completed!" -ForegroundColor Green
