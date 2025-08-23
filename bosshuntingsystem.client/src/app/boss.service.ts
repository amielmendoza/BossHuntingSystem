import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { map, tap } from 'rxjs/operators';

export interface BossDto {
  id: number;
  name: string;
  location: string;
  respawnMinutes: number;
  lastKilledAt: string;
}

export interface BossCreateUpdateDto {
  name: string;
  location: string;
  respawnMinutes: number;
  lastKilledAt: string;
}

export interface BossDefeatDto {
  id: number;
  bossId: number;
  bossName: string;
  location: string;
  defeatedAtUtc: string;
  loots: string[];
  attendees: string[];
}

@Injectable({ providedIn: 'root' })
export class BossService {
  private apiBase: string = '';
  private historyUpdated = new Subject<void>();
  historyUpdated$ = this.historyUpdated.asObservable();

  constructor(private http: HttpClient) {
    // If running under Angular dev server on 53931, call the backend directly (CORS enabled)
    if (location.port === '53931') {
      this.apiBase = 'https://localhost:7294';
    }
  }

  private url(path: string): string { return `${this.apiBase}${path}`; }

  list(): Observable<BossDto[]> { return this.http.get<BossDto[]>(this.url('/api/bosses')); }
  create(payload: BossCreateUpdateDto): Observable<BossDto> { return this.http.post<BossDto>(this.url('/api/bosses'), payload); }
  update(id: number, payload: BossCreateUpdateDto): Observable<BossDto> { return this.http.put<BossDto>(this.url(`/api/bosses/${id}`), payload); }
  defeat(id: number): Observable<BossDto> {
    return this.http.post<BossDto>(this.url(`/api/bosses/${id}/defeat`), {})
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
}


