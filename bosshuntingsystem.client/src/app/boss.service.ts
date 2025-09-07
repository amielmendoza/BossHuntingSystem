import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../environments/environment';

export interface BossDto {
  id: number;
  name: string;
  respawnHours: number;
  lastKilledAt: string; // PHT (Philippine Time) from server
  nextRespawnAt: string; // PHT (Philippine Time) from server
  isAvailable: boolean;
  owner?: string;
}

export interface BossCreateUpdateDto {
  name: string;
  respawnHours: number;
  lastKilledAt: string | null;
  owner?: string;
}

export interface LootItemDto {
  name: string;
  price: number | null;
}

export interface BossDefeatDto {
  id: number;
  bossId: number;
  bossName: string;
  combatPower: string[];
  defeatedAtUtc: string | null; // ISO date string from server, null for history entries
  owner?: string;
  loots: string[];
  attendees: string[];
  lootItems?: LootItemDto[]; // New property for loot with prices
}

export interface MemberDto {
  id: number;
  name: string;
  combatPower: number;
  gcashNumber?: string;
  gcashName?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateUpdateMemberDto {
  name: string;
  combatPower: number;
  gcashNumber?: string;
  gcashName?: string;
}

export interface DefeatBossDto {
  owner?: string;
}

export interface AddHistoryDto {
  owner?: string;
}

export interface MemberPointsDto {
  memberName: string;
  points: number;
  bossesAttended: number;
}

export interface DividendsCalculationRequest {
  totalSales: number;
  startDate?: string;
  endDate?: string;
}

export interface MemberDividendDto {
  memberName: string;
  points: number;
  dividend: number;
}

export interface DividendsCalculationResult {
  totalSales: number;
  totalPoints: number;
  periodStart?: string;
  periodEnd?: string;
  memberDividends: MemberDividendDto[];
  calculatedAt: string;
}





@Injectable({ providedIn: 'root' })
export class BossService {
  private apiBase: string = environment.apiUrl;
  private historyUpdated = new Subject<void>();
  historyUpdated$ = this.historyUpdated.asObservable();

  constructor(private http: HttpClient) {
    // The apiBase is now configured via environment files
    // Development: points to localhost:7294
    // Production: empty string (same domain)
  }

  private url(path: string): string { 
    // Support absolute URLs (http/https) and relative API paths
    const isAbsolute = /^https?:\/\//i.test(path);
    const basePlusPath = isAbsolute ? path : `${this.apiBase}${path}`;
    // Add cache-busting parameter to prevent caching
    const timestamp = Date.now();
    const separator = basePlusPath.includes('?') ? '&' : '?';
    return `${basePlusPath}${separator}_t=${timestamp}`; 
  }

  list(): Observable<BossDto[]> { return this.http.get<BossDto[]>(this.url('/api/bosses')); }
  getById(id: number): Observable<BossDto> { return this.http.get<BossDto>(this.url(`/api/bosses/${id}`)); }
  create(payload: BossCreateUpdateDto): Observable<BossDto> { return this.http.post<BossDto>(this.url('/api/bosses'), payload); }
  update(id: number, payload: BossCreateUpdateDto): Observable<BossDto> { return this.http.put<BossDto>(this.url(`/api/bosses/${id}`), payload); }
  defeat(id: number, owner?: string): Observable<BossDto> {
    const payload = owner ? { owner } : {};
    return this.http.post<BossDto>(this.url(`/api/bosses/${id}/defeat`), payload)
      .pipe(tap(() => this.historyUpdated.next()));
  }
  addHistory(id: number, payload?: { owner?: string; defeatedAt?: string }): Observable<BossDefeatDto> {
    const requestPayload = payload || {};
    return this.http.post<BossDefeatDto>(this.url(`/api/bosses/${id}/add-history`), requestPayload)
      .pipe(tap(() => this.historyUpdated.next()));
  }
  history(): Observable<BossDefeatDto[]> { return this.http.get<BossDefeatDto[]>(this.url('/api/bosses/history')); }
  historyById(id: number): Observable<BossDefeatDto> { return this.http.get<BossDefeatDto>(this.url(`/api/bosses/history/${id}`)); }
  addLoot(historyId: number, text: string): Observable<BossDefeatDto> {
    return this.http.post<BossDefeatDto>(this.url(`/api/bosses/history/${historyId}/loot`), { text });
  }
  addAttendee(historyId: number, text: string): Observable<BossDefeatDto> {
    return this.http.post<BossDefeatDto>(this.url(`/api/bosses/history/${historyId}/attendee`), { text });
  }
  removeLoot(historyId: number, index: number): Observable<BossDefeatDto> {
    return this.http.delete<BossDefeatDto>(this.url(`/api/bosses/history/${historyId}/loot/${index}`));
  }
  removeAttendee(historyId: number, index: number): Observable<BossDefeatDto> {
    return this.http.delete<BossDefeatDto>(this.url(`/api/bosses/history/${historyId}/attendee/${index}`));
  }
  updateLootPrice(historyId: number, index: number, price: number | null): Observable<BossDefeatDto> {
    return this.http.put<BossDefeatDto>(this.url(`/api/bosses/history/${historyId}/loot/${index}/price`), { index, price });
  }

  // Member methods
  getMembers(): Observable<MemberDto[]> {
    return this.http.get<MemberDto[]>(this.url('/api/members'));
  }

  getMember(id: number): Observable<MemberDto> {
    return this.http.get<MemberDto>(this.url(`/api/members/${id}`));
  }

  createMember(member: CreateUpdateMemberDto): Observable<MemberDto> {
    return this.http.post<MemberDto>(this.url('/api/members'), member);
  }

  updateMember(id: number, member: CreateUpdateMemberDto): Observable<MemberDto> {
    return this.http.put<MemberDto>(this.url(`/api/members/${id}`), member);
  }

  deleteMember(id: number): Observable<void> {
    return this.http.delete<void>(this.url(`/api/members/${id}`));
  }


  deleteHistory(historyId: number): Observable<void> {
    return this.http
      .delete(this.url(`/api/bosses/history/${historyId}`), { responseType: 'text' as 'json' })
      .pipe(
        tap(() => this.historyUpdated.next()),
        map(() => void 0)
      );
  }

  // Vision AI extraction
  extractFromImage(file: File, mode: 'loot' | 'attendee'): Observable<{ loots: string[]; attendees: string[] }> {
    const form = new FormData();
    form.append('file', file);
    form.append('mode', mode);
    return this.http.post<{ loots: string[]; attendees: string[] }>(this.url('/api/vision/extract'), form);
  }
  
  // Use text responseType to avoid JSON parse on 204 No Content
  delete(id: number): Observable<void> {
    console.log('[BossService] DELETE request to:', this.url(`/api/bosses/${id}`));
    return this.http
      .delete(this.url(`/api/bosses/${id}`), { responseType: 'text' as 'json' })
      .pipe(
        tap(() => console.log('[BossService] DELETE response received')),
        map(() => void 0)
      );
  }

  sendManualNotification(message: string): Observable<any> {
    return this.http.post(this.url('/api/bosses/notify'), { message });
  }

  // Get member points from attendance tracking
  getMemberPoints(): Observable<MemberPointsDto[]> {
    return this.http.get<MemberPointsDto[]>(this.url('/api/bosses/points'));
  }

  // Calculate dividends based on total sales and member points
  calculateDividends(request: DividendsCalculationRequest): Observable<DividendsCalculationResult> {
    return this.http.post<DividendsCalculationResult>(this.url('/api/bosses/calculate-dividends'), request);
  }


}


