import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AccessControlService {
  private readonly SECRET_KEY = 'bh_admin_access';
  private readonly SECRET_VALUE = 'parak2024';

  constructor() { }

  /**
   * Check if the user has admin access based on session storage
   */
  hasAdminAccess(): boolean {
    try {
      const storedValue = sessionStorage.getItem(this.SECRET_KEY);
      return storedValue === this.SECRET_VALUE;
    } catch (error) {
      console.warn('Session storage not available:', error);
      return false;
    }
  }

  /**
   * Set admin access in session storage
   * This can be called from browser console: window.setAdminAccess()
   */
  setAdminAccess(): boolean {
    try {
      sessionStorage.setItem(this.SECRET_KEY, this.SECRET_VALUE);
      console.log('Admin access granted for this session');
      return true;
    } catch (error) {
      console.error('Failed to set admin access:', error);
      return false;
    }
  }

  /**
   * Remove admin access from session storage
   */
  removeAdminAccess(): void {
    try {
      sessionStorage.removeItem(this.SECRET_KEY);
      console.log('Admin access removed');
    } catch (error) {
      console.error('Failed to remove admin access:', error);
    }
  }

  /**
   * Initialize global access method for browser console
   */
  initGlobalAccessMethod(): void {
    // Make the setAdminAccess method available globally for console access
    (window as any).setAdminAccess = () => {
      const result = this.setAdminAccess();
      if (result) {
        // Trigger a page reload to update the UI
        window.location.reload();
      }
      return result;
    };

    (window as any).removeAdminAccess = () => {
      this.removeAdminAccess();
      // Trigger a page reload to update the UI
      window.location.reload();
    };

    (window as any).checkAdminAccess = () => {
      const hasAccess = this.hasAdminAccess();
      console.log(`Admin access: ${hasAccess ? 'GRANTED' : 'DENIED'}`);
      return hasAccess;
    };

    // Show helpful console message
    console.log(`
🔐 Boss Hunting System - Access Control
=====================================
Commands available:
• setAdminAccess()    - Grant admin access for this session
• removeAdminAccess() - Remove admin access
• checkAdminAccess()  - Check current access status

Current status: ${this.hasAdminAccess() ? 'ADMIN ACCESS GRANTED' : 'PUBLIC ACCESS ONLY'}
`);
  }
}