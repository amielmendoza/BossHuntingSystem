import { Component, OnInit } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { BossService, IpRestrictionInfo } from './boss.service';

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
export class AppComponent implements OnInit {
  public menu: Menu = { isOpen: false };
  public name: Name = { route: '' };
  public ipRestrictionInfo: IpRestrictionInfo | null = null;
  public isLoadingIpCheck = true;

  constructor(private router: Router, private bossService: BossService) {}

  ngOnInit(): void {
    console.log('[BossHunt] AppComponent init');
    this.updateRouteName();
    this.checkIpRestrictions();
    
    // Subscribe to router events to update route name
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.updateRouteName();
    });
  }

  // Check IP restrictions and update UI accordingly
  private checkIpRestrictions(): void {
    this.isLoadingIpCheck = true;
    this.bossService.checkIpRestrictions().subscribe({
      next: (info: IpRestrictionInfo) => {
        console.log('[BossHunt] IP restriction info:', info);
        this.ipRestrictionInfo = info;
        this.isLoadingIpCheck = false;
      },
      error: (error) => {
        console.error('[BossHunt] Failed to check IP restrictions:', error);
        // If we can't check IP restrictions, assume restricted for security
        this.ipRestrictionInfo = {
          clientIp: 'unknown',
          isRestricted: true,
          restrictedEndpoints: [],
          allowedIps: [],
          ipRestrictionsEnabled: true
        };
        this.isLoadingIpCheck = false;
      }
    });
  }

  // Check if user has permission to access restricted features
  public hasRestrictedAccess(): boolean {
    return this.ipRestrictionInfo ? !this.ipRestrictionInfo.isRestricted : false;
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
}
