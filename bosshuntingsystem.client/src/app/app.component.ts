import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { BossService } from './boss.service';
import { AuthService } from './services/auth.service';
import { GlobalAuthService } from './services/global-auth.service';

type Menu = {
  isOpen: boolean;
};

type Name = {
  route: string;
};

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  public menu: Menu = { isOpen: false };
  public name: Name = { route: '' };
  public isAuthenticated = false;
  public currentUser: string | null = null;
  public isCheckingAuth = true;
  public isMaintenanceMode = false;

  private subscriptions = new Subscription();
  private broadcastChannel: BroadcastChannel | null = null;

  constructor(
    private router: Router, 
    private bossService: BossService,
    private authService: AuthService,
    private globalAuthService: GlobalAuthService
  ) {}

  ngOnInit(): void {
    console.log('[BossHunt] AppComponent init');
    this.setupCrossTabCommunication();
    this.updateRouteName();
    
    // Subscribe to router events to update route name
    this.subscriptions.add(
      this.router.events.pipe(
        filter(event => event instanceof NavigationEnd)
      ).subscribe(() => {
        this.updateRouteName();
      })
    );

    // Subscribe to authentication checking state
    this.subscriptions.add(
      this.authService.isCheckingAuth$.subscribe(isChecking => {
        this.isCheckingAuth = isChecking;
      })
    );

    // Subscribe to authentication state
    this.subscriptions.add(
      this.authService.isAuthenticated$.subscribe(isAuth => {
        this.isAuthenticated = isAuth;
        
        // If not authenticated and not already on login page, redirect immediately
        if (!isAuth && this.router.url !== '/login') {
          console.log('[AppComponent] User not authenticated, redirecting to login');
          this.router.navigate(['/login']);
        }
      })
    );

    this.subscriptions.add(
      this.authService.currentUser$.subscribe(user => {
        this.currentUser = user;
      })
    );

    // Subscribe to maintenance mode
    this.subscriptions.add(
      this.globalAuthService.maintenanceMode$.subscribe(isMaintenance => {
        this.isMaintenanceMode = isMaintenance;
        if (isMaintenance) {
          console.log('[AppComponent] Maintenance mode detected, forcing logout');
          this.handleMaintenanceMode();
        }
      })
    );

    // Listen for force logout events
    this.subscriptions.add(
      this.globalAuthService.forceLogout$.subscribe(shouldForceLogout => {
        if (shouldForceLogout) {
          console.log('[AppComponent] Force logout detected, redirecting to login');
          this.handleForceLogout();
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.broadcastChannel) {
      this.broadcastChannel.close();
    }
  }

  /**
   * Setup cross-tab communication for forced logout
   */
  private setupCrossTabCommunication(): void {
    try {
      // Use BroadcastChannel API if available
      if ('BroadcastChannel' in window) {
        this.broadcastChannel = new BroadcastChannel('auth_channel');
        this.broadcastChannel.onmessage = (event) => {
          if (event.data.type === 'FORCE_LOGOUT') {
            console.log('[AppComponent] Force logout message received from another tab');
            this.handleForceLogout();
          }
        };
      }

      // Fallback to localStorage events
      window.addEventListener('storage', (event) => {
        if (event.key === 'force_logout') {
          console.log('[AppComponent] Force logout event detected from localStorage');
          this.handleForceLogout();
        }
      });
    } catch (error) {
      console.error('[AppComponent] Error setting up cross-tab communication:', error);
    }
  }

  /**
   * Handle maintenance mode
   */
  private handleMaintenanceMode(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  /**
   * Handle forced logout
   */
  private handleForceLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  // Update route name for display
  updateRouteName(): void {
    const currentRoute = this.router.url;
    if (currentRoute === '/' || currentRoute === '/dashboard') {
      this.name.route = 'Dashboard';
    } else if (currentRoute === '/history') {
      this.name.route = 'History';
    } else if (currentRoute === '/members') {
      this.name.route = 'Members';
    } else if (currentRoute === '/notifications') {
      this.name.route = 'Notifications';
    } else {
      this.name.route = 'Unknown';
    }
  }

  openMenu(): void {
    this.menu.isOpen = !this.menu.isOpen;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  // Force logout all users (admin function)
  forceLogoutAllUsers(): void {
    this.globalAuthService.forceLogoutAllUsers();
    this.globalAuthService.broadcastForceLogout();
    this.router.navigate(['/login']);
  }

  // Enable maintenance mode (admin function)
  enableMaintenanceMode(): void {
    this.globalAuthService.enableMaintenanceMode();
    this.globalAuthService.broadcastForceLogout();
  }

  // Disable maintenance mode (admin function)
  disableMaintenanceMode(): void {
    this.globalAuthService.disableMaintenanceMode();
  }
}
