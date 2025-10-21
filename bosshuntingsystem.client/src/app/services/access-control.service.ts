import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AccessControlService {
  constructor() { }

  /**
   * Check if the user has admin access based on JWT role
   * This now reads directly from localStorage to avoid circular dependency
   */
  hasAdminAccess(): boolean {
    try {
      const userJson = localStorage.getItem('user_info');
      if (!userJson) {
        return false;
      }
      const user = JSON.parse(userJson);
      // Admin and SuperAdmin roles have admin access
      return user.role === 'Admin' || user.role === 'SuperAdmin';
    } catch (error) {
      return false;
    }
  }

  /**
   * Check if the user has manager access (can see all pages except admin-only pages)
   */
  hasManagerAccess(): boolean {
    try {
      const userJson = localStorage.getItem('user_info');
      if (!userJson) {
        return false;
      }
      const user = JSON.parse(userJson);
      // Manager, Admin, and SuperAdmin roles have manager-level access
      return user.role === 'Manager' || user.role === 'Admin' || user.role === 'SuperAdmin';
    } catch (error) {
      return false;
    }
  }

  /**
   * Initialize console logging (removed emergency access methods for security)
   */
  initGlobalAccessMethod(): void {
    // Show system info in console
    console.log(`
🔐 Boss Hunting System - Multi-Tenant Edition
==============================================
Authentication: JWT-based with role-based access control
Roles: SuperAdmin, Admin, Manager, User

Access is now managed through proper authentication.
Login at /login to access the system.
`);
  }
}