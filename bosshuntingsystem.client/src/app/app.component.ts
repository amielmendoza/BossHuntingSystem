import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { interval, Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { BossService, BossDto, BossCreateUpdateDto } from './boss.service';



// Removed WeatherForecast usage from the dashboard

type Boss = {
  id: number;
  name: string;
  respawnHours: number;
  lastKilledAt: Date;
  nextRespawnAt: Date;
  isAvailable: boolean;
};

type Menu = {
  isOpen: boolean;
};

type Name = {
  route: string;
};

type Modal = {
  isOpen: boolean;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  public bosses: Boss[] = [];
  public menu: Menu = { isOpen: false };
  public name: Name = { route: '' };
  private timerSubscription?: Subscription;
  private nowEpochMs: number = Date.now();

  constructor(private http: HttpClient, private bossApi: BossService, private router: Router) {}

  ngOnInit(): void {
    console.log('[BossHunt] AppComponent init');
    this.loadBosses();
    this.updateRouteName();
    
    // Subscribe to router events to update route name
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.updateRouteName();
    });
    
    this.timerSubscription = interval(1000).subscribe(() => {
      this.nowEpochMs = Date.now();
    });
  }

  ngOnDestroy(): void {
    this.timerSubscription?.unsubscribe();
  }

  // Update route name for display
  updateRouteName(): void {
    const currentRoute = this.router.url;
    if (currentRoute === '/' || currentRoute === '') {
      this.name.route = 'Dashboard';
    } else if (currentRoute === '/history') {
      this.name.route = 'History';
    } else if (currentRoute === '/notifications') {
      this.name.route = 'Notifications';
    } else {
      this.name.route = 'Unknown';
    }
  }

  // Routing helper: show dashboard content only on base route
  isDashboardRoute(): boolean {
    return this.router.url === '/' || this.router.url === '';
  }

  // Helper to get current PHT time in format suitable for datetime-local input
  private getPhtNowForInput(): string {
    // Get current time and format for datetime-local input (no timezone offset)
    const now = new Date();
    // Adjust for PHT (UTC+8) - this is a simplified approach for the input
    const phtOffset = 8 * 60; // PHT is UTC+8
    const phtTime = new Date(now.getTime() + phtOffset * 60 * 1000);
    return phtTime.toISOString().slice(0, 16); // Remove seconds and Z
  }



  private loadBosses(): void {
    console.log('[BossHunt] Requesting /api/bosses');
    this.bossApi.list().subscribe({
      next: (rows: BossDto[]) => {
        console.log('[BossHunt] /api/bosses OK', rows?.length);
        console.log('[BossHunt] Boss IDs received:', rows?.map(r => r.id));
        this.bosses = rows.map(r => ({
          id: r.id,
          name: r.name,
          respawnHours: r.respawnHours,
          lastKilledAt: r.lastKilledAt ? new Date(r.lastKilledAt) : new Date(),
          nextRespawnAt: r.nextRespawnAt ? new Date(r.nextRespawnAt) : new Date(),
          isAvailable: r.isAvailable
        }));
        console.log('[BossHunt] Updated local bosses array, count:', this.bosses.length);
      },
      error: (err) => {
        console.error('[BossHunt] Failed to load bosses', err);
        // Extra surface for troubleshooting
        try {
          // eslint-disable-next-line no-console
          console.log('[BossHunt] location', window.location.href);
        } catch {}
      }
    });
  }

  // CRUD actions
  public editModel: { id?: number; name: string; respawnHours: number; lastKilledAt: string } =
    { name: '', respawnHours: 1, lastKilledAt: '' };

  startNew(): void {
    this.editModel = { name: '', respawnHours: 1, lastKilledAt: '' };
  }

  startEdit(boss: Boss): void {
    // Call API to get the latest data from server
    this.bossApi.getById(boss.id).subscribe({
      next: (freshBoss) => {
        // The server sends UTC time, convert to local time for datetime-local input
        const utcDate = new Date(freshBoss.lastKilledAt);
        const localDateTime = new Date(utcDate.getTime() - (utcDate.getTimezoneOffset() * 60000));
        
        this.editModel = {
          id: freshBoss.id,
          name: freshBoss.name,
          respawnHours: freshBoss.respawnHours,
          lastKilledAt: localDateTime.toISOString().slice(0, 16) // Format for datetime-local in local time
        };
      },
      error: (e) => {
        console.error('Error fetching boss data for edit:', e);
        // Fallback to using the UI data if API call fails
        const utcDate = new Date(boss.lastKilledAt);
        const localDateTime = new Date(utcDate.getTime() - (utcDate.getTimezoneOffset() * 60000));
        
        this.editModel = {
          id: boss.id,
          name: boss.name,
          respawnHours: boss.respawnHours,
          lastKilledAt: localDateTime.toISOString().slice(0, 16)
        };
      }
    });
  }

  saveBoss(): void {
    let lastKilledAtValue: string | null = null;
    
    // Convert local datetime-local input back to UTC ISO string for server
    if (this.editModel.lastKilledAt.trim() !== '') {
      const localDate = new Date(this.editModel.lastKilledAt);
      lastKilledAtValue = localDate.toISOString();
    }
    
    const payload: BossCreateUpdateDto = {
      name: this.editModel.name,
      respawnHours: this.editModel.respawnHours,
      lastKilledAt: lastKilledAtValue
    };
    const onDone = () => { this.startNew(); this.loadBosses(); };
    if (this.editModel.id) {
      this.bossApi.update(this.editModel.id, payload).subscribe({ next: onDone, error: (e) => console.error(e) });
    } else {
      this.bossApi.create(payload).subscribe({ next: onDone, error: (e) => console.error(e) });
    }
  }

  deleteBoss(id: number): void {
    console.log('[BossHunt] Deleting boss with ID:', id);
    if (confirm('Are you sure you want to delete this boss?')) {
      this.bossApi.delete(id).subscribe({ 
        next: () => {
          console.log('[BossHunt] Boss deleted successfully');
          // Remove from local array immediately for responsive UI
          this.bosses = this.bosses.filter(b => b.id !== id);
          console.log('[BossHunt] Removed from local array, remaining bosses:', this.bosses.length);
          // Also refresh from server to ensure consistency
          this.loadBosses();
        }, 
        error: (e) => {
          console.error('[BossHunt] Error deleting boss:', e);
        }
      });
    }
  }

  defeat(boss: Boss): void {
    if (!boss.isAvailable) { return; }
    this.bossApi.defeat(boss.id).subscribe({
      next: (updated) => {
        // Update in place to keep UI responsive
        boss.lastKilledAt = updated.lastKilledAt ? new Date(updated.lastKilledAt) : new Date();
        boss.nextRespawnAt = updated.nextRespawnAt ? new Date(updated.nextRespawnAt) : new Date();
        boss.isAvailable = updated.isAvailable;
      },
      error: (e) => console.error(e)
    });
  }

  addHistory(boss: Boss): void {
    this.bossApi.addHistory(boss.id).subscribe({
      next: (historyRecord) => {
        console.log('History record added:', historyRecord);
        // No need to update boss data since respawn timer doesn't reset
      },
      error: (e) => console.error(e)
    });
  }

  openMenu(): void {
    this.menu.isOpen = !this.menu.isOpen;
  }

  // Boss timer helpers (now use server-calculated values)
  nextRespawnAt(boss: Boss): Date {
    return boss.nextRespawnAt || new Date();
  }

  msUntilRespawn(boss: Boss): number {
    return (boss.nextRespawnAt || new Date()).getTime() - this.nowEpochMs;
  }

  isAvailable(boss: Boss): boolean {
    // For real-time updates, check current time against nextRespawnAt
    return this.msUntilRespawn(boss) <= 0;
  }

  countdownText(boss: Boss): string {
    const ms = this.msUntilRespawn(boss);
    if (ms <= 0) {
      return 'Available';
    }
    return this.formatDuration(ms);
  }

  private formatDuration(ms: number): string {
    const totalSeconds = Math.floor(ms / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    const pad = (n: number) => String(n).padStart(2, '0');
    if (hours > 0) {
      return `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`;
    }
    return `${pad(minutes)}:${pad(seconds)}`;
  }
}
