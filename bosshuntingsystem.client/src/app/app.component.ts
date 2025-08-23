import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { interval, Subscription } from 'rxjs';
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

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  public bosses: Boss[] = [];
  private timerSubscription?: Subscription;
  private nowEpochMs: number = Date.now();

  constructor(private http: HttpClient, private bossApi: BossService, private router: Router) {}

  ngOnInit(): void {
    console.log('[BossHunt] AppComponent init');
    this.loadBosses();
    this.timerSubscription = interval(1000).subscribe(() => {
      this.nowEpochMs = Date.now();
    });
  }

  ngOnDestroy(): void {
    this.timerSubscription?.unsubscribe();
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
        this.bosses = rows.map(r => ({
          id: r.id,
          name: r.name,
          respawnHours: r.respawnHours,
          lastKilledAt: r.lastKilledAt ? new Date(r.lastKilledAt) : new Date(),
          nextRespawnAt: r.nextRespawnAt ? new Date(r.nextRespawnAt) : new Date(),
          isAvailable: r.isAvailable
        }));
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
    this.editModel = {
      id: boss.id,
      name: boss.name,
      respawnHours: boss.respawnHours,
      lastKilledAt: boss.lastKilledAt.toISOString().slice(0, 16) // Format for datetime-local
    };
  }

  saveBoss(): void {
    const payload: BossCreateUpdateDto = {
      name: this.editModel.name,
      respawnHours: this.editModel.respawnHours,
      lastKilledAt: this.editModel.lastKilledAt.trim() === '' ? null : this.editModel.lastKilledAt
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
