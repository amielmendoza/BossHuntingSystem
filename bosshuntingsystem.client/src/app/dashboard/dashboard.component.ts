import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { interval, Subscription } from 'rxjs';
import { BossService, BossDto, BossCreateUpdateDto, IpRestrictionInfo } from '../boss.service';
import { DateUtilsService } from '../utils/date-utils.service';

type Boss = {
  id: number;
  name: string;
  respawnHours: number;
  lastKilledAt: Date;
  nextRespawnAt: Date;
  isAvailable: boolean;
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
  
  // IP restriction state
  ipRestrictionInfo: IpRestrictionInfo | null = null;
  isIpRestricted = false;

  constructor(
    private http: HttpClient, 
    private bossApi: BossService, 
    private _dateUtils: DateUtilsService
  ) {
    this.dateUtils = _dateUtils;
  }

  ngOnInit(): void {
    console.log('[BossHunt] DashboardComponent init');
    // Check IP restrictions first
    this.checkIpRestrictions();
    this.loadBosses();
    this.timerSubscription = interval(1000).subscribe(() => {
      this.nowEpochMs = Date.now();
    });
  }

  ngOnDestroy(): void {
    this.timerSubscription?.unsubscribe();
  }

  checkIpRestrictions(): void {
    this.bossApi.checkIpRestrictions().subscribe({
      next: (info) => {
        this.ipRestrictionInfo = info;
        this.isIpRestricted = info.isRestricted;
        console.log('[Dashboard] IP restriction check:', info);
        console.log('[Dashboard] Client IP:', info.clientIp);
        console.log('[Dashboard] Is Restricted:', info.isRestricted);
      },
      error: (e) => {
        console.error('Failed to check IP restrictions', e);
        // If we can't check, assume restricted for security
        this.isIpRestricted = true;
      }
    });
  }

  // Check if user has permission to access restricted features
  public hasRestrictedAccess(): boolean {
    return !this.isIpRestricted;
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
          lastKilledAt: this._dateUtils.utcToLocal(r.lastKilledAt) || new Date(),
          nextRespawnAt: this._dateUtils.utcToLocal(r.nextRespawnAt) || new Date(),
          isAvailable: r.isAvailable
        }));
        console.log('[BossHunt] Updated local bosses array, count:', this.bosses.length);
      },
      error: (err) => {
        console.error('[BossHunt] Failed to load bosses', err);
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
    this.bossApi.getById(boss.id).subscribe({
      next: (freshBoss) => {
        const localDate = this._dateUtils.utcToLocal(freshBoss.lastKilledAt);
        
        this.editModel = {
          id: freshBoss.id,
          name: freshBoss.name,
          respawnHours: freshBoss.respawnHours,
          lastKilledAt: localDate ? localDate.toISOString().slice(0, 16) : ''
        };
      },
      error: (e) => {
        console.error('Error fetching boss data for edit:', e);
        const localDate = this._dateUtils.utcToLocal(boss.lastKilledAt);
        
        this.editModel = {
          id: boss.id,
          name: boss.name,
          respawnHours: boss.respawnHours,
          lastKilledAt: localDate ? localDate.toISOString().slice(0, 16) : ''
        };
      }
    });
  }

  saveBoss(): void {
    let lastKilledAtValue: string | null = null;
    
    if (this.editModel.lastKilledAt.trim() !== '') {
      const localDate = new Date(this.editModel.lastKilledAt);
      lastKilledAtValue = this._dateUtils.localToUtc(localDate);
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
    if (!boss.isAvailable) { return; }
    this.bossApi.defeat(boss.id).subscribe({
      next: (updated) => {
        boss.lastKilledAt = this._dateUtils.utcToLocal(updated.lastKilledAt) || new Date();
        boss.nextRespawnAt = this._dateUtils.utcToLocal(updated.nextRespawnAt) || new Date();
        boss.isAvailable = updated.isAvailable;
      },
      error: (e) => console.error(e)
    });
  }

  addHistory(boss: Boss): void {
    this.bossApi.addHistory(boss.id).subscribe({
      next: (historyRecord) => {
        console.log('History record added:', historyRecord);
      },
      error: (e) => console.error(e)
    });
  }

  // Boss timer helpers
  nextRespawnAt(boss: Boss): Date {
    return boss.nextRespawnAt || new Date();
  }

  msUntilRespawn(boss: Boss): number {
    return (boss.nextRespawnAt || new Date()).getTime() - this.nowEpochMs;
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
