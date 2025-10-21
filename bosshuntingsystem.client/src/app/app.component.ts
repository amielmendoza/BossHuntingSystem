import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { BossService } from './boss.service';
import { AccessControlService } from './services/access-control.service';
import { AuthService } from './services/auth.service';

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
  public hasAdminAccess = false;
  public hasManagerAccess = false;
  public isLoggedIn = false;
  public isAdmin = false;
  public isSuperAdmin = false;
  public isManager = false;

  private subscriptions = new Subscription();

  constructor(
    private router: Router,
    private bossService: BossService,
    private accessControl: AccessControlService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    console.log('[BossHunt] AppComponent init');

    // Initialize access control and global methods
    this.updateAdminAccess();
    this.updateLoginStatus();
    this.accessControl.initGlobalAccessMethod();

    this.updateRouteName();

    // Subscribe to router events to update route name and access
    this.subscriptions.add(
      this.router.events.pipe(
        filter(event => event instanceof NavigationEnd)
      ).subscribe(() => {
        this.updateRouteName();
        this.updateAdminAccess();
        this.updateLoginStatus();
      })
    );

    // Subscribe to auth status changes
    this.subscriptions.add(
      this.authService.currentUser$.subscribe(() => {
        this.updateLoginStatus();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  // Update admin access status
  private updateAdminAccess(): void {
    this.hasAdminAccess = this.accessControl.hasAdminAccess();
    this.hasManagerAccess = this.accessControl.hasManagerAccess();
  }

  // Update login status
  private updateLoginStatus(): void {
    this.isLoggedIn = this.authService.isLoggedIn();
    if (this.isLoggedIn) {
      const user = this.authService.getCurrentUser();
      this.isAdmin = user?.role === 'Admin';
      this.isSuperAdmin = user?.role === 'SuperAdmin';
      this.isManager = user?.role === 'Manager';
    } else {
      this.isAdmin = false;
      this.isSuperAdmin = false;
      this.isManager = false;
    }
  }

  // Logout
  logout(): void {
    this.authService.logout();
  }

  // Update route name for display
  updateRouteName(): void {
    const currentRoute = this.router.url;
    if (currentRoute === '/' || currentRoute === '/dashboard') {
      this.name.route = 'Dashboard';
    } else if (currentRoute === '/login') {
      this.name.route = 'Login';
    } else if (currentRoute === '/license-expired') {
      this.name.route = 'License Expired';
    } else if (currentRoute === '/history') {
      this.name.route = 'History';
    } else if (currentRoute === '/members') {
      this.name.route = 'Members';
    } else if (currentRoute === '/member-deletion') {
      this.name.route = 'Delete Records';
    } else if (currentRoute === '/points') {
      this.name.route = 'Points';
    } else if (currentRoute === '/notifications') {
      this.name.route = 'Notifications';
    } else if (currentRoute === '/jae') {
      this.name.route = 'JAE';
    } else if (currentRoute.startsWith('/admin/clients')) {
      this.name.route = 'Client Management';
    } else if (currentRoute.startsWith('/admin/audit-logs')) {
      this.name.route = 'Audit Logs';
    } else if (currentRoute.startsWith('/admin/users')) {
      this.name.route = 'User Management';
    } else {
      this.name.route = 'Boss Hunting System';
    }
  }

  openMenu(): void {
    this.menu.isOpen = !this.menu.isOpen;
  }

  // Check if current route requires admin access
  isRestrictedRoute(): boolean {
    const currentRoute = this.router.url;
    const restrictedRoutes = ['/history', '/members', '/points', '/notifications'];
    return restrictedRoutes.some(route => currentRoute.startsWith(route));
  }
}
