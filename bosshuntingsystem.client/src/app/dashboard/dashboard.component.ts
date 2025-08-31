import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { interval, Subscription } from 'rxjs';
import { BossService, BossDto, BossCreateUpdateDto } from '../boss.service';
import { DateUtilsService } from '../utils/date-utils.service';

type Boss = {
  id: number;
  name: string;
  respawnHours: number;
  lastKilledAt: Date; // PHT (Philippine Time) from backend
  nextRespawnAt: Date; // PHT (Philippine Time) from backend
  killer?: string;
};

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  public bosses: Boss[] = [];
  public dateUtils: DateUtilsService;
  private timerSubscription?: Subscription;
  private nowEpochMs: number = Date.now();
  

  
  // Loading states
  public historyLoadingStates: { [bossId: number]: boolean } = {};

  constructor(
    private http: HttpClient, 
    private bossApi: BossService, 
    private _dateUtils: DateUtilsService
  ) {
    this.dateUtils = _dateUtils;
    // Initialize with current time (backend already returns PHT times)
    this.nowEpochMs = Date.now();
  }

  ngOnInit(): void {
    console.log('[BossHunt] DashboardComponent init');
    this.loadBosses();
    this.timerSubscription = interval(1000).subscribe(() => {
      this.nowEpochMs = Date.now();
    });
  }

  ngOnDestroy(): void {
    this.timerSubscription?.unsubscribe();
  }

  private loadBosses(): void {
    console.log('[BossHunt] Requesting /api/bosses');
    this.bossApi.list().subscribe({
      next: (rows: BossDto[]) => {
        console.log('[BossHunt] /api/bosses OK', rows?.length);
        console.log('[BossHunt] Boss IDs received:', rows?.map(r => r.id));
        this.bosses = rows.map(r => {
          // Backend sends PHT dates directly - no timezone conversion needed
          const nextRespawnAt = new Date(r.nextRespawnAt);
          const lastKilledAt = new Date(r.lastKilledAt);
          
          // Debug logging for availability calculation
          if (r.name === 'Venatus') {
            const msUntilRespawn = nextRespawnAt.getTime() - this.nowEpochMs;
            const currentPhtTime = this._dateUtils.getCurrentPhtTime();
            console.log(`[DEBUG] ${r.name}:`, {
              originalDate: r.nextRespawnAt,
              parsedDate: nextRespawnAt.toISOString(),
              currentTime: new Date(this.nowEpochMs).toISOString(),
              currentPhtTime: currentPhtTime.toISOString(),
              diffMs: msUntilRespawn,
              diffHours: msUntilRespawn / (1000 * 60 * 60),
              diffDays: msUntilRespawn / (1000 * 60 * 60 * 24),
              calculatedIsAvailable: msUntilRespawn <= 0,
              phtDiffMs: nextRespawnAt.getTime() - currentPhtTime.getTime(),
              phtDiffHours: (nextRespawnAt.getTime() - currentPhtTime.getTime()) / (1000 * 60 * 60)
            });
          }
          
          return {
            id: r.id,
            name: r.name,
            respawnHours: r.respawnHours,
            lastKilledAt: lastKilledAt,
            nextRespawnAt: nextRespawnAt,
            killer: r.killer
          };
        });
        console.log('[BossHunt] Updated local bosses array, count:', this.bosses.length);
      },
      error: (err) => {
        console.error('[BossHunt] Failed to load bosses', err);
      }
    });
  }

  // CRUD actions
  public editModel: { id?: number; name: string; respawnHours: number; lastKilledAt: string; killer?: string } =
    { name: '', respawnHours: 1, lastKilledAt: '', killer: '' };

  startNew(): void {
    this.editModel = { name: '', respawnHours: 1, lastKilledAt: '', killer: '' };
  }

  startEdit(boss: Boss): void {
    this.bossApi.getById(boss.id).subscribe({
      next: (freshBoss) => {
        const phtDate = new Date(freshBoss.lastKilledAt);
        
        this.editModel = {
          id: freshBoss.id,
          name: freshBoss.name,
          respawnHours: freshBoss.respawnHours,
          lastKilledAt: phtDate ? this._dateUtils.phtToDatetimeLocal(phtDate) : '',
          killer: freshBoss.killer || ''
        };
      },
      error: (e) => {
        console.error('Error fetching boss data for edit:', e);
        const phtDate = boss.lastKilledAt;
        
        this.editModel = {
          id: boss.id,
          name: boss.name,
          respawnHours: boss.respawnHours,
          lastKilledAt: phtDate ? this._dateUtils.phtToDatetimeLocal(phtDate) : '',
          killer: boss.killer || ''
        };
      }
    });
  }

  saveBoss(): void {
    let lastKilledAtValue: string | null = null;
    
    if (this.editModel.lastKilledAt.trim() !== '') {
      // The datetime-local input provides a PHT time string
      // Send PHT time directly to backend - let backend convert to UTC
      const phtDate = this._dateUtils.datetimeLocalToPht(this.editModel.lastKilledAt);
      // Format as PHT time string for backend
      const year = phtDate.getFullYear();
      const month = String(phtDate.getMonth() + 1).padStart(2, '0');
      const day = String(phtDate.getDate()).padStart(2, '0');
      const hours = String(phtDate.getHours()).padStart(2, '0');
      const minutes = String(phtDate.getMinutes()).padStart(2, '0');
      const seconds = String(phtDate.getSeconds()).padStart(2, '0');
      lastKilledAtValue = `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
    }
    
    const payload: BossCreateUpdateDto = {
      name: this.editModel.name,
      respawnHours: this.editModel.respawnHours,
      lastKilledAt: lastKilledAtValue,
      killer: this.editModel.killer?.trim() || undefined
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
          this.bosses = this.bosses.filter(b => b.id !== id);
          this.loadBosses();
        }, 
        error: (e) => {
          console.error('[BossHunt] Error deleting boss:', e);
        }
      });
    }
  }

  defeat(boss: Boss): void {
    if (!this.isAvailable(boss)) { return; }
    
    // Use the current killer value from the boss record
    const killer = boss.killer;
    
    this.bossApi.defeat(boss.id, killer || undefined).subscribe({
      next: (updated) => {
        // Backend already returns PHT dates, no conversion needed
        const nextRespawnAt = new Date(updated.nextRespawnAt);
        const lastKilledAt = new Date(updated.lastKilledAt);
        
        boss.lastKilledAt = lastKilledAt;
        boss.nextRespawnAt = nextRespawnAt;
        boss.killer = updated.killer;
      },
      error: (e) => console.error(e)
    });
  }

  addHistory(boss: Boss): void {
    // Prevent double-clicking
    if (this.historyLoadingStates[boss.id]) {
      console.log('[Dashboard] History request already in progress for boss:', boss.id);
      return;
    }
    
    // Use the current killer value from the boss record
    const killer = boss.killer;
    
    // Get current PHT time for the defeated at timestamp
    const currentPhtTime = this._dateUtils.getCurrentPhtTime();
    const year = currentPhtTime.getFullYear();
    const month = String(currentPhtTime.getMonth() + 1).padStart(2, '0');
    const day = String(currentPhtTime.getDate()).padStart(2, '0');
    const hours = String(currentPhtTime.getHours()).padStart(2, '0');
    const minutes = String(currentPhtTime.getMinutes()).padStart(2, '0');
    const seconds = String(currentPhtTime.getSeconds()).padStart(2, '0');
    const defeatedAtPht = `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
    
    console.log('[Dashboard] Adding history for boss:', boss.id, boss.name, 'at PHT time:', defeatedAtPht);
    this.historyLoadingStates[boss.id] = true;
    
    // Create payload with current PHT time
    const payload = {
      killer: killer?.trim() || undefined,
      defeatedAt: defeatedAtPht
    };
    
    this.bossApi.addHistory(boss.id, payload).subscribe({
      next: (historyRecord) => {
        console.log('[Dashboard] History record added successfully:', historyRecord);
        this.historyLoadingStates[boss.id] = false;
      },
      error: (e) => {
        console.error('[Dashboard] Error adding history:', e);
        this.historyLoadingStates[boss.id] = false;
      }
    });
  }

  // Boss timer helpers
  nextRespawnAt(boss: Boss): Date {
    return boss.nextRespawnAt || this._dateUtils.getCurrentPhtTime();
  }

  msUntilRespawn(boss: Boss): number {
    const nextRespawnTime = boss.nextRespawnAt || this._dateUtils.getCurrentPhtTime();
    const currentTime = this.nowEpochMs;
    return nextRespawnTime.getTime() - currentTime;
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
