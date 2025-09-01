import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class GlobalAuthService {
  private forceLogoutSubject = new BehaviorSubject<boolean>(false);
  private maintenanceModeSubject = new BehaviorSubject<boolean>(false);

  public forceLogout$ = this.forceLogoutSubject.asObservable();
  public maintenanceMode$ = this.maintenanceModeSubject.asObservable();

  constructor(private authService: AuthService) {
    // Listen for force logout events
    this.forceLogout$.subscribe(shouldForceLogout => {
      if (shouldForceLogout) {
        this.executeForceLogout();
      }
    });
  }

  /**
   * Force logout all users from the system
   * This can be called by admins or triggered by system events
   */
  forceLogoutAllUsers(): void {
    console.log('[GlobalAuthService] Force logout all users triggered');
    this.forceLogoutSubject.next(true);
  }

  /**
   * Enable maintenance mode - this will force all users to logout
   */
  enableMaintenanceMode(): void {
    console.log('[GlobalAuthService] Maintenance mode enabled');
    this.maintenanceModeSubject.next(true);
    this.forceLogoutAllUsers();
  }

  /**
   * Disable maintenance mode
   */
  disableMaintenanceMode(): void {
    console.log('[GlobalAuthService] Maintenance mode disabled');
    this.maintenanceModeSubject.next(false);
  }

  /**
   * Check if system is in maintenance mode
   */
  isMaintenanceMode(): boolean {
    return this.maintenanceModeSubject.value;
  }

  /**
   * Execute the actual force logout
   */
  private executeForceLogout(): void {
    try {
      // Clear all authentication data
      this.authService.forceLogoutAllUsers();
      
      // Clear any other session-related data
      this.clearAllSessionData();
      
      // Reset the force logout flag
      this.forceLogoutSubject.next(false);
      
      console.log('[GlobalAuthService] Force logout executed successfully');
    } catch (error) {
      console.error('[GlobalAuthService] Error during force logout:', error);
    }
  }

  /**
   * Clear all session-related data from localStorage and sessionStorage
   */
  private clearAllSessionData(): void {
    // Clear localStorage
    const localStorageKeys = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && (
        key.startsWith('auth_') || 
        key.includes('token') || 
        key.includes('user') ||
        key.includes('session') ||
        key.includes('cache')
      )) {
        localStorageKeys.push(key);
      }
    }
    
    localStorageKeys.forEach(key => {
      localStorage.removeItem(key);
      console.log(`[GlobalAuthService] Cleared localStorage key: ${key}`);
    });

    // Clear sessionStorage
    const sessionStorageKeys = [];
    for (let i = 0; i < sessionStorage.length; i++) {
      const key = sessionStorage.key(i);
      if (key && (
        key.startsWith('auth_') || 
        key.includes('token') || 
        key.includes('user') ||
        key.includes('session') ||
        key.includes('cache')
      )) {
        sessionStorageKeys.push(key);
      }
    }
    
    sessionStorageKeys.forEach(key => {
      sessionStorage.removeItem(key);
      console.log(`[GlobalAuthService] Cleared sessionStorage key: ${key}`);
    });
  }

  /**
   * Broadcast force logout to all tabs/windows of the same origin
   */
  broadcastForceLogout(): void {
    try {
      // Use BroadcastChannel API if available
      if ('BroadcastChannel' in window) {
        const channel = new BroadcastChannel('auth_channel');
        channel.postMessage({ type: 'FORCE_LOGOUT', timestamp: Date.now() });
        channel.close();
      }
      
      // Fallback to localStorage event
      localStorage.setItem('force_logout', Date.now().toString());
      localStorage.removeItem('force_logout');
      
      console.log('[GlobalAuthService] Force logout broadcast sent');
    } catch (error) {
      console.error('[GlobalAuthService] Error broadcasting force logout:', error);
    }
  }
}


