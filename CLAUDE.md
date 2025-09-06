# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Architecture

**BossHuntingSystem** is a full-stack web application built with Angular (frontend) and ASP.NET Core (backend) for tracking boss encounters in gaming guilds/groups. The system supports authentication, member management, boss tracking, notifications, and image OCR capabilities.

### Core Structure
- `BossHuntingSystem.Server/` - ASP.NET Core 8.0 Web API backend
- `bosshuntingsystem.client/` - Angular 17 frontend application
- Solution file: `BossHuntingSystem.sln` (main entry point)

### Backend Components
- **Controllers**: AuthController, BossesController, MembersController, VisionController, TestController
- **Data Layer**: Entity Framework with SQL Server (`Data/BossHuntingDbContext`)
- **Authentication**: JWT Bearer token authentication with role-based authorization (Admin/User roles)
- **Services**: Vision API integration (Azure), Discord notifications
- **Authorization Policies**: Granular permission system (Admin, User, ReadOnly, Write, BossManagement, etc.)

### Frontend Components
- **Angular 17** with TypeScript, Bootstrap 5.3, ng-bootstrap
- **Key Areas**: dashboard, history, login, members, notifications
- **Services**: boss.service.ts, authentication, API interceptors
- **Guards**: emergency-auth.guard.ts for route protection
- **Features**: OCR text extraction with tesseract.js, responsive design

## Common Development Commands

### Frontend (Angular)
```bash
cd bosshuntingsystem.client

# Development server with SSL
npm start
# or specifically:
npm run start:windows    # Windows with SSL certs
npm run start:default    # Unix/Mac with SSL certs

# Build for different environments
npm run build            # Development build
npm run build:prod       # Production build
npm run build:staging    # Staging build

# Testing
npm test                 # Run unit tests
npm run watch            # Build and watch for changes

# Install dependencies
npm install
```

### Backend (.NET)
```bash
cd BossHuntingSystem.Server

# Restore and build
dotnet restore
dotnet build

# Run the application
dotnet run

# Database operations (Entity Framework)
dotnet ef migrations add <MigrationName>
dotnet ef database update

# Publish for production
dotnet publish --configuration Release --output ./publish
```

### Full Solution Build
```bash
# Build entire solution
dotnet build BossHuntingSystem.sln

# Visual Studio solution build
msbuild BossHuntingSystem.sln /p:Configuration=Release
```

## Development Setup

### Prerequisites
- .NET 8.0 SDK
- Node.js (LTS version)
- SQL Server (or SQL Server Express for development)

### Local Development
1. **Backend**: The server runs on `https://localhost:7228` by default
2. **Frontend**: Angular dev server runs on `https://localhost:53931`
3. **Proxy Configuration**: `src/proxy.conf.js` routes API calls to backend
4. **SSL Certificates**: Configured for HTTPS development (required for production-like testing)

### Database
- **Connection String**: Configured in `appsettings.Development.json` and `appsettings.Production.json`
- **Migrations**: Located in `BossHuntingSystem.Server/Migrations/`
- **Context**: `BossHuntingDbContext` with entities for Boss, Member, BossDefeat

### Authentication
- **JWT Configuration**: `appsettings.json` contains JWT settings (SecretKey, Issuer, Audience, ExpirationMinutes)
- **Default Users**: Admin (admin/admin123) and User (user/user123) roles
- **Token Management**: Frontend handles token storage and refresh logic

## Deployment

### Production Deployment Options
1. **Vercel/Netlify**: Frontend-only deployment with API mocking
2. **Windows Server/IIS**: Full-stack deployment (see `WINDOWS_SERVER_DEPLOYMENT.md`)
3. **Cloud Providers**: Azure, AWS with containerization

### Build Output
- **Angular Build**: Outputs to `BossHuntingSystem.Server/wwwroot` for integrated deployment
- **.NET Publish**: Self-contained or framework-dependent deployment options

### Configuration Files
- **Angular Environment**: `src/environments/environment.*.ts` files for different deployment targets
- **.NET Configuration**: `appsettings.*.json` files for environment-specific settings
- **CORS Configuration**: Backend configured to allow specific origins (update for production domains)

## External Integrations

### Azure Vision API
- **Purpose**: OCR text extraction from uploaded images
- **Configuration**: `AZURE_VISION_ENDPOINT` and `AZURE_VISION_API_KEY` in appsettings
- **Usage**: `/api/vision/extract` endpoint for image processing

### Discord Notifications
- **Purpose**: Automated boss defeat notifications
- **Configuration**: `DISCORD_WEBHOOK_URL` in appsettings
- **Usage**: Webhook-based notifications for guild/team updates

## Security Considerations

### Authentication & Authorization
- **JWT Bearer Tokens**: Stateless authentication
- **Role-Based Access**: Admin and User roles with different permission levels
- **Policy-Based Authorization**: Granular permissions (BossManagement, MemberManagement, etc.)
- **Route Guards**: Frontend guards prevent unauthorized access to protected routes

### Data Protection
- **Input Validation**: All API endpoints validate input data
- **SQL Injection Prevention**: Entity Framework provides parameterized queries
- **CORS Configuration**: Restrictive CORS policy for production environments

## Testing

### Frontend Testing
- **Framework**: Jasmine with Karma test runner
- **Command**: `npm test` runs unit tests
- **Configuration**: `karma.conf.js` and `tsconfig.spec.json`

### Backend Testing
- **Framework**: Built-in .NET testing capabilities
- **Location**: No specific test projects found in current structure
- **API Testing**: Use `BossHuntingSystem.Server.http` file for manual API testing

## Special Files and Scripts

### Deployment Scripts
- `deploy-windows-server.ps1` - Automated Windows Server deployment
- `clean-build.ps1` - Clean build script
- `fix-angular-compilation.ps1` - Angular compilation fixes

### Configuration Files
- `vercel.json` - Vercel deployment configuration
- `web.config` - IIS deployment configuration
- `.deployment` - Azure deployment settings

## Development Tips

1. **SSL Requirements**: Both frontend and backend are configured for HTTPS development
2. **Database First**: Entity Framework migrations handle database schema changes
3. **Environment Configuration**: Update environment files when changing API endpoints or external service URLs
4. **Build Order**: Frontend builds into backend's wwwroot for integrated deployment
5. **Authorization Testing**: Use the provided admin/user accounts for testing different permission levels