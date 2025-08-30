import { Component, OnInit } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { BossService } from './boss.service';

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

  constructor(private router: Router, private bossService: BossService) {}

  ngOnInit(): void {
    console.log('[BossHunt] AppComponent init');
    this.updateRouteName();
    
    // Subscribe to router events to update route name
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.updateRouteName();
    });
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
