# 🚀 Windows Server Deployment Guide for BossHuntingSystem

## 📋 **Overview**
This guide provides step-by-step instructions for deploying the BossHuntingSystem application to a Windows Server environment using IIS and SQL Server.

## 🎯 **System Requirements**

### **Server Requirements**
- **Windows Server 2019/2022** (recommended)
- **Minimum 4GB RAM** (8GB recommended)
- **50GB available disk space**
- **Static IP address** (for production)

### **Software Prerequisites**
- **.NET 8.0 Runtime** and **SDK**
- **SQL Server** (Express, Standard, or Enterprise)
- **IIS (Internet Information Services)** with ASP.NET Core Hosting Bundle
- **Node.js** (for building Angular frontend)

## 🔧 **Step 1: Install Required Software**

### **Install .NET 8.0**
1. Download .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Run the installer as Administrator
3. Verify installation:
   ```powershell
   dotnet --version
   ```

### **Install SQL Server**
1. Download SQL Server from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Choose **SQL Server Express** (free) for development/testing
3. Choose **SQL Server Standard/Enterprise** for production
4. Install with **Mixed Mode Authentication**
5. Note down the **SA password** and **Server name**

### **Install IIS with ASP.NET Core Hosting Bundle**
```powershell
# Enable IIS features
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging
Enable-WindowsOptionalFeature -Online -FeatureName IIS-RequestFiltering
Enable-WindowsOptionalFeature -Online -FeatureName IIS-StaticContent
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DefaultDocument
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DirectoryBrowsing
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebSockets
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45

# Install ASP.NET Core Hosting Bundle
# Download from: https://dotnet.microsoft.com/download/dotnet/8.0
```

### **Install Node.js**
1. Download Node.js LTS from: https://nodejs.org/
2. Run the installer
3. Verify installation:
   ```powershell
   node --version
   npm --version
   ```

## 🗄️ **Step 2: Configure SQL Server Database**

### **Create Database**
1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your SQL Server instance
3. Create a new database:
   ```sql
   CREATE DATABASE BossHuntingSystem;
   GO
   ```

### **Configure Connection String**
Update the connection string in `appsettings.Production.json`:

**For Windows Authentication:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=BossHuntingSystem;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
}
```

**For SQL Server Authentication:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=BossHuntingSystem;User Id=sa;Password=YourPassword;TrustServerCertificate=true;MultipleActiveResultSets=true"
}
```

## 🌐 **Step 3: Configure Domain and CORS**

### **Update CORS Settings**
Edit `Program.cs` and update the production CORS configuration:

```csharp
else
{
    // Production: Allow your Windows Server domain
    policy.WithOrigins(
            "https://your-server-domain.com",
            "http://your-server-domain.com",
            "https://localhost",
            "http://localhost")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
}
```

Replace `your-server-domain.com` with your actual domain name.

## 🚀 **Step 4: Deploy the Application**

### **Option A: Automated Deployment (Recommended)**
1. Run the deployment script as Administrator:
   ```powershell
   .\deploy-windows-server.ps1
   ```

2. The script will:
   - Check prerequisites
   - Build the Angular frontend
   - Publish the .NET application
   - Configure IIS Application Pool
   - Create IIS Website
   - Set file permissions
   - Create web.config

### **Option B: Manual Deployment**

#### **Build Angular Frontend**
```powershell
cd bosshuntingsystem.client
npm install
npm run build:prod
```

#### **Publish .NET Application**
```powershell
cd BossHuntingSystem.Server
dotnet restore
dotnet publish --configuration Release --output C:\inetpub\wwwroot\BossHuntingSystem --self-contained false
```

#### **Copy Angular Build Output**
```powershell
Copy-Item -Path "bosshuntingsystem.client\dist\bosshuntingsystem.client\browser\*" -Destination "C:\inetpub\wwwroot\BossHuntingSystem\wwwroot" -Recurse -Force
```

#### **Configure IIS**
1. Open **IIS Manager**
2. Create Application Pool:
   - Name: `BossHuntingSystem`
   - .NET CLR Version: `No Managed Code`
   - Identity: `ApplicationPoolIdentity`

3. Create Website:
   - Name: `BossHuntingSystem`
   - Physical Path: `C:\inetpub\wwwroot\BossHuntingSystem`
   - Application Pool: `BossHuntingSystem`
   - Port: `80`

#### **Set File Permissions**
```powershell
$acl = Get-Acl "C:\inetpub\wwwroot\BossHuntingSystem"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl -Path "C:\inetpub\wwwroot\BossHuntingSystem" -AclObject $acl
```

## 🔒 **Step 5: Configure SSL/HTTPS**

### **Install SSL Certificate**
1. Obtain an SSL certificate from a trusted CA
2. Install the certificate in Windows Certificate Store
3. Bind the certificate to your IIS website:
   - Open IIS Manager
   - Select your website
   - Click "Bindings"
   - Add HTTPS binding with your certificate

### **Update CORS for HTTPS**
Ensure your CORS configuration includes the HTTPS domain.

## 🔧 **Step 6: Configure Environment Variables**

### **Production Settings**
Update `appsettings.Production.json` with your production values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BossHuntingSystem;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "AZURE_VISION_ENDPOINT": "https://your-vision-endpoint.cognitiveservices.azure.com/",
  "AZURE_VISION_API_KEY": "your-vision-api-key",
  "DISCORD_WEBHOOK_URL": "your-discord-webhook-url",
  "IpRestrictions": {
    "Enabled": true,
    "AllowedIps": ["your-server-ip"],
    "RestrictedEndpoints": [
      "POST:/api/bosses/history/*/loot",
      "POST:/api/bosses/history/*/attendee",
      "DELETE:/api/bosses/history/*/loot/*",
      "DELETE:/api/bosses/history/*/attendee/*",
      "DELETE:/api/bosses/history/*"
    ]
  }
}
```

## 🧪 **Step 7: Test the Deployment**

### **Verify Application**
1. Open browser and navigate to: `http://localhost`
2. Test all major features:
   - Dashboard
   - Boss history
   - Member management
   - Notifications

### **Check Logs**
- Application logs: `C:\inetpub\wwwroot\BossHuntingSystem\logs\`
- IIS logs: `C:\inetpub\logs\LogFiles\`
- Event Viewer: Windows Logs > Application

## 🔄 **Step 8: Set Up Continuous Deployment (Optional)**

### **Using PowerShell Scripts**
Create a scheduled task to run the deployment script:

```powershell
# Create scheduled task for deployment
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-File C:\path\to\deploy-windows-server.ps1"
$trigger = New-ScheduledTaskTrigger -AtStartup
Register-ScheduledTask -TaskName "BossHuntingSystem-Deploy" -Action $action -Trigger $trigger -User "SYSTEM" -RunLevel Highest
```

### **Using Git Hooks**
Set up post-receive hooks for automatic deployment when code is pushed.

## 🛠️ **Troubleshooting**

### **Common Issues**

#### **500.19 Error - Configuration Error**
- Ensure ASP.NET Core Hosting Bundle is installed
- Check web.config file exists and is valid

#### **500.30 Error - Startup Error**
- Check application logs in `logs\` directory
- Verify connection string is correct
- Ensure database exists and is accessible

#### **404 Error - File Not Found**
- Verify Angular build files are in `wwwroot\` directory
- Check IIS static file handling is enabled

#### **CORS Errors**
- Update CORS configuration in `Program.cs`
- Ensure domain names are correct
- Check for typos in allowed origins

### **Performance Optimization**

#### **Enable Compression**
Add to `web.config`:
```xml
<httpCompression>
  <dynamicTypes>
    <add mimeType="application/json" enabled="true" />
    <add mimeType="text/html" enabled="true" />
    <add mimeType="text/css" enabled="true" />
    <add mimeType="application/javascript" enabled="true" />
  </dynamicTypes>
</httpCompression>
```

#### **Enable Caching**
Add to `web.config`:
```xml
<staticContent>
  <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
</staticContent>
```

## 📊 **Monitoring and Maintenance**

### **Health Checks**
- Set up application health monitoring
- Configure alerts for downtime
- Monitor database performance

### **Backup Strategy**
- Regular database backups
- Application file backups
- Configuration backups

### **Updates**
- Keep .NET runtime updated
- Update Angular dependencies
- Monitor security patches

## 🎉 **Deployment Complete!**

Your BossHuntingSystem is now deployed and running on Windows Server. The application should be accessible at your configured domain or IP address.

### **Next Steps**
1. Configure DNS to point to your server
2. Set up monitoring and alerting
3. Implement backup strategies
4. Plan for scaling and maintenance

---

**📞 Support**: If you encounter issues, check the application logs and IIS logs for detailed error information.

