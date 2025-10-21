# SSL Certificate Setup for devdrix.com

## The Issue
You're getting `ERR_CERT_COMMON_NAME_INVALID` because the current SSL certificate was issued for `risingforcedev.store` but your domain is now `devdrix.com`.

## Solutions

### Option 1: Get a New SSL Certificate (Recommended for Production)

#### For Windows Server with IIS:
1. **Request a new SSL certificate for devdrix.com:**
   - Use Let's Encrypt (free): https://letsencrypt.org/
   - Use Certbot for Windows: https://certbot.eff.org/
   - Or purchase from a CA like DigiCert, Comodo, etc.

2. **Install the certificate:**
   ```powershell
   # Using Certbot
   certbot --iis -d devdrix.com
   ```

3. **Update IIS bindings:**
   - Open IIS Manager
   - Select your site
   - Click "Bindings"
   - Update HTTPS binding to use the new certificate

#### For reverse proxy (nginx/Apache):
Update your SSL certificate paths in the configuration to point to the new devdrix.com certificate.

### Option 2: Temporary Development Workaround

#### For Chrome/Edge (Development only):
1. Navigate to `https://devdrix.com`
2. Click "Advanced" when you see the certificate error
3. Click "Proceed to devdrix.com (unsafe)"

#### For programmatic access:
Add this to your development environment (NOT for production):

```javascript
// In Angular interceptor (development only)
if (environment.production === false) {
  process.env["NODE_TLS_REJECT_UNAUTHORIZED"] = "0";
}
```

### Option 3: Use HTTP for Development
Update environment.staging.ts to use HTTP instead of HTTPS:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://devdrix.com'  // Note: HTTP instead of HTTPS
};
```

## DNS Configuration
Ensure your domain DNS is pointing to the correct server IP:
```
A Record: devdrix.com -> YOUR_SERVER_IP
```

## Next Steps
1. ✅ Updated CORS configuration to allow devdrix.com
2. ✅ Updated Angular environment files
3. ⏳ Get new SSL certificate for devdrix.com
4. ⏳ Update server SSL configuration