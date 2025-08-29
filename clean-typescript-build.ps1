# Clean TypeScript Build Script for Angular Project
# This script clears all TypeScript and Angular caches and rebuilds the project

Write-Host "🧹 Cleaning TypeScript and Angular build cache..." -ForegroundColor Yellow

# Navigate to the Angular client directory
Set-Location "bosshuntingsystem.client"

# Clear Angular cache
Write-Host "📦 Clearing Angular cache..." -ForegroundColor Yellow
if (Test-Path ".angular") {
    Remove-Item -Recurse -Force ".angular"
    Write-Host "✅ Angular cache cleared" -ForegroundColor Green
}

# Clear TypeScript cache
Write-Host "📦 Clearing TypeScript cache..." -ForegroundColor Yellow
if (Test-Path "node_modules\.cache") {
    Remove-Item -Recurse -Force "node_modules\.cache"
    Write-Host "✅ TypeScript cache cleared" -ForegroundColor Green
}

# Clear npm cache
Write-Host "📦 Clearing npm cache..." -ForegroundColor Yellow
npm cache clean --force
Write-Host "✅ npm cache cleared" -ForegroundColor Green

# Clear node_modules and reinstall (optional - uncomment if needed)
# Write-Host "📦 Clearing node_modules..." -ForegroundColor Yellow
# if (Test-Path "node_modules") {
#     Remove-Item -Recurse -Force "node_modules"
#     Write-Host "✅ node_modules cleared" -ForegroundColor Green
#     Write-Host "📦 Reinstalling packages..." -ForegroundColor Yellow
#     npm install
# }

# Build for production with verbose output
Write-Host "🏗️ Building Angular application for production..." -ForegroundColor Yellow
npm run build:prod

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Write-Host "💡 Try running with verbose output:" -ForegroundColor Yellow
    Write-Host "   ng build --configuration=production --verbose" -ForegroundColor White
    exit 1
}

Write-Host "🎉 Clean TypeScript build completed!" -ForegroundColor Green
