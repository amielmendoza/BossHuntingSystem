# Forced Logout System Guide

This guide explains how to use the new forced logout system that allows administrators to immediately redirect all users to the login page.

## Overview

The forced logout system provides multiple ways to ensure all users are immediately redirected to the login page:

1. **Immediate Authentication Check** - Users without valid tokens are redirected immediately
2. **Force Logout All Users** - Admin function to force all users to logout
3. **Maintenance Mode** - System-wide maintenance mode that forces all users to logout
4. **Cross-Tab Communication** - Force logout works across all browser tabs/windows
5. **Emergency Route Protection** - Additional route guards for emergency situations

## How It Works

### 1. Immediate Authentication Check

When a user loads the website:
- The system immediately checks for authentication tokens
- If no valid tokens are found, the user is redirected to login immediately
- No protected content is shown before the redirect
- A loading spinner is displayed during authentication checks

### 2. Force Logout All Users

**Button Location**: Top-right corner of the dashboard (orange "Force Logout All" button)

**What it does**:
- Clears all authentication data from localStorage and sessionStorage
- Resets authentication state for all users
- Broadcasts the logout event to all open tabs/windows
- Redirects the current user to login

**When to use**:
- Security incidents
- System updates
- User session management
- Emergency situations

### 3. Maintenance Mode

**Button Location**: Top-right corner of the dashboard (yellow "Maintenance Mode" button)

**What it does**:
- Enables system-wide maintenance mode
- Forces all users to logout immediately
- Shows maintenance warning message
- Prevents access to all protected routes
- Can be disabled to resume normal operation

**When to use**:
- Scheduled maintenance
- System updates
- Database migrations
- Emergency system work

### 4. Cross-Tab Communication

The system automatically handles force logout across multiple browser tabs:
- Uses BroadcastChannel API when available
- Falls back to localStorage events
- Ensures all tabs receive the logout command
- Prevents users from staying logged in on other tabs

## Implementation Details

### Services

- **AuthService**: Handles individual user authentication
- **GlobalAuthService**: Manages system-wide authentication state and forced logout

### Guards

- **AuthGuard**: Protects routes from unauthorized access
- **EmergencyAuthGuard**: Blocks access during maintenance mode

### Components

- **AppComponent**: Main application component with authentication controls
- **LoginComponent**: Login form for re-authentication

## Usage Examples

### Force Logout All Users

```typescript
// In any component
constructor(private globalAuthService: GlobalAuthService) {}

forceLogoutAll(): void {
  this.globalAuthService.forceLogoutAllUsers();
  this.globalAuthService.broadcastForceLogout();
}
```

### Enable Maintenance Mode

```typescript
// Enable maintenance mode
this.globalAuthService.enableMaintenanceMode();

// Disable maintenance mode
this.globalAuthService.disableMaintenanceMode();
```

### Check Maintenance Status

```typescript
// Subscribe to maintenance mode changes
this.globalAuthService.maintenanceMode$.subscribe(isMaintenance => {
  if (isMaintenance) {
    console.log('System is in maintenance mode');
  }
});
```

## Security Features

1. **Immediate Token Validation**: No delay in authentication checks
2. **Comprehensive Data Clearing**: Removes all authentication-related data
3. **Cross-Tab Synchronization**: Ensures logout across all browser sessions
4. **Route Protection**: Multiple layers of route guards
5. **Audit Logging**: All force logout actions are logged to console

## Best Practices

1. **Use Force Logout All** for immediate security incidents
2. **Use Maintenance Mode** for planned system work
3. **Communicate with users** before enabling maintenance mode
4. **Monitor logs** for any authentication issues
5. **Test the system** in a development environment first

## Troubleshooting

### Users Still See Protected Content

1. Check browser console for error messages
2. Verify that the AuthGuard is properly configured
3. Ensure the EmergencyAuthGuard is working
4. Check if there are any route configuration issues

### Force Logout Not Working

1. Verify the GlobalAuthService is properly injected
2. Check browser console for errors
3. Ensure localStorage is accessible
4. Verify the broadcast mechanism is working

### Maintenance Mode Issues

1. Check if maintenance mode is properly enabled
2. Verify the maintenance mode state in the service
3. Check if the UI is properly reflecting the state
4. Ensure all components are subscribed to maintenance mode changes

## API Endpoints

The system uses these backend endpoints:

- `POST /api/auth/login` - User login
- `POST /api/auth/validate` - Token validation

## Browser Compatibility

- **Modern Browsers**: Full support with BroadcastChannel API
- **Older Browsers**: Fallback to localStorage events
- **Mobile Browsers**: Full support
- **Private/Incognito Mode**: May have limited localStorage support

## Performance Considerations

- Authentication checks are performed immediately on app load
- Token validation is cached to minimize API calls
- Cross-tab communication is optimized for minimal overhead
- Route guards are lightweight and efficient

## Future Enhancements

Potential improvements for the forced logout system:

1. **Server-side forced logout** - Force logout from backend
2. **User notification system** - Inform users before forced logout
3. **Scheduled maintenance** - Automated maintenance mode scheduling
4. **Audit trail** - Track all force logout actions
5. **Role-based access** - Limit force logout to specific user roles
6. **Geographic restrictions** - Force logout users in specific regions
7. **Device management** - Force logout specific devices or sessions


