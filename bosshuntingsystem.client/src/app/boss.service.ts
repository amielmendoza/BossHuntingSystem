import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../environments/environment';

export interface BossDto {
  id: number;
  name: string;
  respawnHours: number;
  lastKilledAt: string; // UTC time from server
  nextRespawnAt: string; // UTC time from server
  isAvailable: boolean;
}

export interface BossCreateUpdateDto {
  name: string;
  respawnHours: number;
  lastKilledAt: string | null;
}

export interface BossDefeatDto {
  id: number;
  bossId: number;
  bossName: string;
  defeatedAtUtc: string | null; // ISO date string from server, null for history entries
  loots: string[];
  attendees: string[];
}

@Injectable({ providedIn: 'root' })
export class BossService {
  private apiBase: string = environment.apiBaseUrl;
  private historyUpdated = new Subject<void>();
  historyUpdated$ = this.historyUpdated.asObservable();

  constructor(private http: HttpClient) {
    // The apiBase is now configured via environment files
    // Development: points to localhost:7294
    // Production: empty string (same domain)
  }

  private url(path: string): string { 
    // Add cache-busting parameter to prevent caching
    const timestamp = new Date().getTime();
    const separator = path.includes('?') ? '&' : '?';
    return `${this.apiBase}${path}${separator}_t=${timestamp}`; 
  }

  list(): Observable<BossDto[]> { return this.http.get<BossDto[]>(this.url('/api/bosses')); }
  getById(id: number): Observable<BossDto> { return this.http.get<BossDto>(this.url(`/api/bosses/${id}`)); }
  create(payload: BossCreateUpdateDto): Observable<BossDto> { return this.http.post<BossDto>(this.url('/api/bosses'), payload); }
  update(id: number, payload: BossCreateUpdateDto): Observable<BossDto> { return this.http.put<BossDto>(this.url(`/api/bosses/${id}`), payload); }
  defeat(id: number): Observable<BossDto> {
    return this.http.post<BossDto>(this.url(`/api/bosses/${id}/defeat`), {})
      .pipe(tap(() => this.historyUpdated.next()));
  }
  addHistory(id: number): Observable<BossDefeatDto> {
    return this.http.post<BossDefeatDto>(this.url(`/api/bosses/${id}/add-history`), {})
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
}


