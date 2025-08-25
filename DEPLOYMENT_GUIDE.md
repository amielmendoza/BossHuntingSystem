# Boss Hunting System - Deployment Guide

## How the UI Connects to the Backend

### Development Environment
- **Frontend**: Angular app runs on `https://localhost:53931`
- **Backend**: ASP.NET Core API runs on `https://localhost:7294`
- **Connection**: Frontend makes API calls to `https://localhost:7294/api/*`
- **CORS**: Configured to allow requests from `https://localhost:53931`

### Production Environment (Azure Deployment)
- **Frontend & Backend**: Both served from the same domain (e.g., `https://bosshuntingsystem.azurewebsites.net`)
- **Connection**: Frontend makes API calls to the same domain (`/api/*`)
- **CORS**: Configured to allow requests from the Azure domain

## Configuration Files

### Environment Configuration
- `src/environments/environment.ts` - Development settings (localhost:7294)
- `src/environments/environment.staging.ts` - Staging settings (Azure backend)
- `src/environments/environment.prod.ts` - Production settings (same domain)

### Key Changes Made
1. **BossService**: Now uses environment configuration instead of hardcoded URLs
2. **Angular Build**: Configured to replace environment files during production build
3. **CORS**: Server configured to allow both development and production origins

## Deployment Process

### Manual Deployment

#### For Production (same domain deployment):
```bash
cd bosshuntingsystem.client
npm run build:prod
```

#### For Testing against Azure Backend:
```bash
cd bosshuntingsystem.client
npm run build:staging
```

2. **Deploy to Azure**: The built files are automatically copied to `../BossHuntingSystem.Server/wwwroot`

3. **Server Configuration**: The ASP.NET Core app serves both the API and the static Angular files

### Automated Deployment (Azure)
The `deploy.cmd` script automatically:
1. Restores NuGet packages
2. Builds and publishes the ASP.NET Core application
3. Installs NPM packages
4. **Builds Angular with production configuration** (`npm run build:prod`)
5. Deploys everything to Azure

## Troubleshooting

### If API calls fail in production:
1. Check browser console for CORS errors
2. Verify the domain is included in the CORS policy in `Program.cs`
3. Ensure the Angular app is built with production configuration

### If the app doesn't load:
1. Check that `index.html` is being served correctly
2. Verify static file middleware is configured properly
3. Check Azure Web App configuration and logs

### Verification Steps After Deployment:
1. **Check build output**: Verify that the Angular build created files in `BossHuntingSystem.Server/wwwroot`
2. **Verify environment**: Check that `environment.prod.ts` was used (apiBaseUrl should be empty)
3. **Test API connectivity**: Ensure the frontend can connect to the backend API
4. **Check browser console**: Look for any JavaScript errors or failed API calls
