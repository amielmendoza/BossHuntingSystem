# Security Configuration Guide

## ⚠️ CRITICAL: Secrets Management

**DO NOT** commit production secrets to source control!

### Current Issues

The following files contain sensitive information and should be secured:

1. `appsettings.json` - Contains database credentials, JWT secret, API keys
2. `appsettings.Production.json` - Production configuration

### Immediate Actions Required

#### 1. Rotate All Exposed Credentials

**IMMEDIATELY** change:
- Database password for user `parak`
- JWT SecretKey
- Azure Vision API key (if still valid)
- Discord webhook URLs

#### 2. Move Secrets to Environment Variables

For **Production** deployments:

**Windows Server / IIS:**
```powershell
# Set environment variables
[System.Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=YOUR_SERVER;Database=BOSSHUNTDB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true;", "Machine")
[System.Environment]::SetEnvironmentVariable("JWT__SecretKey", "YOUR_SECURE_RANDOM_KEY_AT_LEAST_32_CHARACTERS", "Machine")
[System.Environment]::SetEnvironmentVariable("AZURE_VISION_API_KEY", "YOUR_AZURE_KEY", "Machine")
[System.Environment]::SetEnvironmentVariable("DISCORD_WEBHOOK_URL_PARAK", "YOUR_WEBHOOK_URL", "Machine")
```

**Linux / Docker:**
```bash
export ConnectionStrings__DefaultConnection="Server=YOUR_SERVER;Database=BOSSHUNTDB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true;"
export JWT__SecretKey="YOUR_SECURE_RANDOM_KEY_AT_LEAST_32_CHARACTERS"
export AZURE_VISION_API_KEY="YOUR_AZURE_KEY"
export DISCORD_WEBHOOK_URL_PARAK="YOUR_WEBHOOK_URL"
```

**Azure App Service:**
- Go to Configuration > Application settings
- Add each setting as a new application setting
- Use `__` (double underscore) to represent nested configuration (e.g., `JWT__SecretKey`)

#### 3. Update appsettings.json for Development

Create `appsettings.Development.json` (add to .gitignore):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BOSSHUNTDB_DEV;User Id=dev_user;Password=dev_password;TrustServerCertificate=true;"
  },
  "JWT": {
    "SecretKey": "Development_Key_DO_NOT_USE_IN_PRODUCTION_123456789",
    "Issuer": "BossHuntingSystem",
    "Audience": "BossHuntingSystemUsers",
    "ExpirationMinutes": 480
  },
  "AZURE_VISION_API_KEY": "development_key",
  "AZURE_VISION_ENDPOINT": "https://development.cognitiveservices.azure.com/",
  "DISCORD_WEBHOOK_URL": ""
}
```

#### 4. Update .gitignore

Add to `.gitignore`:
```
appsettings.Development.json
appsettings.Production.json
appsettings.Staging.json
*.user
*.secrets.json
```

#### 5. Use User Secrets for Development

```bash
cd BossHuntingSystem.Server
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_DEV_CONNECTION_STRING"
dotnet user-secrets set "JWT:SecretKey" "YOUR_DEV_JWT_SECRET"
```

### Generating Secure JWT Secret

Use a cryptographically secure random string generator:

```powershell
# PowerShell
-join ((65..90) + (97..122) + (48..57) + 33,35,36,37,38,42,43,45,46,61,63,64 | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

```bash
# Linux/Mac
openssl rand -base64 48
```

### Azure Key Vault (Recommended for Production)

1. Create Azure Key Vault
2. Add secrets to Key Vault
3. Grant App Service managed identity access to Key Vault
4. Update Program.cs:

```csharp
var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVaultName"];
    var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");

    builder.Configuration.AddAzureKeyVault(
        keyVaultUri,
        new DefaultAzureCredential());
}
```

### Verification Checklist

- [ ] All production secrets rotated
- [ ] Environment variables configured on production server
- [ ] `.gitignore` updated
- [ ] `appsettings.Development.json` created and added to `.gitignore`
- [ ] User secrets configured for local development
- [ ] Tested application startup with environment variables
- [ ] Verified no secrets in git history
- [ ] Team notified of new configuration process

### Database Security

**Additional recommendations:**

1. Use a dedicated database user with minimal permissions
2. Enable SQL Server authentication mode
3. Use SSL/TLS for database connections (remove `TrustServerCertificate=true` in production)
4. Implement database firewall rules
5. Enable database auditing
6. Regular security patches

### API Key Security

**Azure Vision API:**
- Rotate keys regularly (every 90 days)
- Use Azure Managed Identity when possible
- Monitor API usage for unusual patterns
- Set up billing alerts

**Discord Webhooks:**
- Regenerate webhook URLs if exposed
- Use per-environment webhooks
- Monitor webhook activity

### Emergency Response

If secrets are exposed:

1. **Immediate**: Rotate all affected credentials
2. **Within 1 hour**: Review access logs for unauthorized access
3. **Within 24 hours**: Audit all client data for tampering
4. **Within 1 week**: Complete security audit
5. **Document**: Incident report and lessons learned

### Contact

For security concerns: [Your security contact email]
