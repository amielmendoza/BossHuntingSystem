import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { BossService } from './boss.service';
import { AccessControlService } from './services/access-control.service';

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

  private subscriptions = new Subscription();

  constructor(
    private router: Router, 
    private bossService: BossService,
    private accessControl: AccessControlService
  ) {}

  ngOnInit(): void {
    console.log('[BossHunt] AppComponent init');
    
    // Initialize access control and global methods
    this.updateAdminAccess();
    this.accessControl.initGlobalAccessMethod();
    
    this.updateRouteName();
    
    // Subscribe to router events to update route name and access
    this.subscriptions.add(
      this.router.events.pipe(
        filter(event => event instanceof NavigationEnd)
      ).subscribe(() => {
        this.updateRouteName();
        this.updateAdminAccess();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  // Update admin access status
  private updateAdminAccess(): void {
    this.hasAdminAccess = this.accessControl.hasAdminAccess();
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

  // Check if current route requires admin access
  isRestrictedRoute(): boolean {
    const currentRoute = this.router.url;
    const restrictedRoutes = ['/history', '/members', '/notifications'];
    return restrictedRoutes.some(route => currentRoute.startsWith(route));
  }
}
