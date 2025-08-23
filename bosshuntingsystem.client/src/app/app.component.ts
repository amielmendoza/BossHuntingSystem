import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { interval, Subscription } from 'rxjs';
import { BossService, BossDto, BossCreateUpdateDto } from './boss.service';



// Removed WeatherForecast usage from the dashboard

type Boss = {
  id: number;
  name: string;
  location: string;
  respawnMinutes: number;
  lastKilledAt: Date;
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



  private loadBosses(): void {
    console.log('[BossHunt] Requesting /api/bosses');
    this.bossApi.list().subscribe({
      next: (rows: BossDto[]) => {
        console.log('[BossHunt] /api/bosses OK', rows?.length);
        this.bosses = rows.map(r => ({
          id: r.id,
          name: r.name,
          location: r.location,
          respawnMinutes: r.respawnMinutes,
          lastKilledAt: new Date(r.lastKilledAt)
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
  public editModel: { id?: number; name: string; location: string; respawnMinutes: number; lastKilledAt: string } =
    { name: '', location: '', respawnMinutes: 30, lastKilledAt: new Date().toISOString() };

  startNew(): void {
    this.editModel = { name: '', location: '', respawnMinutes: 30, lastKilledAt: new Date().toISOString() };
  }

  startEdit(boss: Boss): void {
    this.editModel = {
      id: boss.id,
      name: boss.name,
      location: boss.location,
      respawnMinutes: boss.respawnMinutes,
      lastKilledAt: boss.lastKilledAt.toISOString()
    };
  }

  saveBoss(): void {
    const payload: BossCreateUpdateDto = {
      name: this.editModel.name,
      location: this.editModel.location,
      respawnMinutes: this.editModel.respawnMinutes,
      lastKilledAt: this.editModel.lastKilledAt
    };
    const onDone = () => { this.startNew(); this.loadBosses(); };
    if (this.editModel.id) {
      this.bossApi.update(this.editModel.id, payload).subscribe({ next: onDone, error: (e) => console.error(e) });
    } else {
      this.bossApi.create(payload).subscribe({ next: onDone, error: (e) => console.error(e) });
    }
  }

  deleteBoss(id: number): void {
    this.bossApi.delete(id).subscribe({ next: () => this.loadBosses(), error: (e) => console.error(e) });
  }

  defeat(boss: Boss): void {
    if (!this.isAvailable(boss)) { return; }
    this.bossApi.defeat(boss.id).subscribe({
      next: (updated) => {
        // Update in place to keep UI responsive
        boss.lastKilledAt = new Date(updated.lastKilledAt);
      },
      error: (e) => console.error(e)
    });
  }

  // Boss timer helpers
  nextRespawnAt(boss: Boss): Date {
    const next = new Date(boss.lastKilledAt.getTime() + boss.respawnMinutes * 60_000);
    return next;
  }

  msUntilRespawn(boss: Boss): number {
    const next = this.nextRespawnAt(boss).getTime();
    return next - this.nowEpochMs;
  }

  isAvailable(boss: Boss): boolean {
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
