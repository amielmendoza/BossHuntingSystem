import { Component, OnDestroy, OnInit } from '@angular/core';
import { BossDefeatDto, MemberDto, BossService, IpRestrictionInfo } from '../boss.service';
import { Subscription, firstValueFrom } from 'rxjs';
import Tesseract from 'tesseract.js';

@Component({
  selector: 'app-history',
  templateUrl: './history.component.html',
  styleUrls: ['./history.component.css']
})
export class HistoryComponent implements OnInit, OnDestroy {
  members: MemberDto[] = [];
  rows: BossDefeatDto[] = [];
  loading = true;
  private sub?: Subscription;
  // Modal state
  modalOpen = false;
  modalMode: 'loot' | 'attendee' = 'loot';
  modalValue = '';
  activeRow: BossDefeatDto | null = null;
  detailsOpen = false;
  details: BossDefeatDto | null = null;
  ocrLoading = false;
  ocrSuggestions: string[] = [];
  ocrAddedCount = 0;
  
  // IP restriction state
  ipRestrictionInfo: IpRestrictionInfo | null = null;
  isIpRestricted = false;

  constructor(private bossApi: BossService) {}

  ngOnInit(): void {
    // Check IP restrictions first
    this.checkIpRestrictions();
    this.loadMembers();
    
    const load = () => this.bossApi.history().subscribe({
      next: r => { this.rows = r; this.loading = false; },
      error: e => { console.error('Failed to load history', e); this.loading = false; }
    });
    load();
    this.sub = this.bossApi.historyUpdated$.subscribe(() => load());
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  checkIpRestrictions(): void {
    this.bossApi.checkIpRestrictions().subscribe({
      next: (info) => {
        this.ipRestrictionInfo = info;
        // Check if any restricted endpoints are being accessed
        this.isIpRestricted = info.isRestricted;
        console.log('[History] IP restriction check:', info);
      },
      error: (e) => {
        console.error('Failed to check IP restrictions', e);
        // If we can't check, assume not restricted to be safe
        this.isIpRestricted = false;
      }
    });
  }

  checkCp(): void {
    if (this.details?.attendees) {
      const cpValues: number[] = [];
      this.details.attendees.forEach((attendee) => {
        const match = this.members.find(m => m.name.toLowerCase() === attendee.toLowerCase());
        if (typeof match?.combatPower === 'number') {
          cpValues.push(match.combatPower);
        } else {
          cpValues.push(0)
        }
      });
      (this.details as any).combatPower = cpValues;
    }
    console.log(this.details);
  }

  openModal(row: BossDefeatDto, mode: 'loot' | 'attendee'): void {
    this.activeRow = row;
    this.modalMode = mode;
    this.modalValue = '';
    // If we are in the details modal, switch to the add form within the same modal
    if (this.detailsOpen) {
      this.detailsOpen = false;
    }
    this.modalOpen = true;
  }

  closeModal(): void { this.modalOpen = false; }

  openDetails(row: BossDefeatDto): void {
    this.bossApi.historyById(row.id).subscribe({
      next: r => { this.details = r; this.detailsOpen = true; this.checkCp(); },
      error: e => console.error('Failed to load details', e)
    });
  }

  closeDetails(): void { this.detailsOpen = false; }

  submitModal(): void {
    const text = this.modalValue.trim();
    const row = this.activeRow;
    if (!row || !text) { return; }
    if (this.modalMode === 'loot') {
      this.bossApi.addLoot(row.id, text).subscribe({
        next: (updated) => { 
          row.loots = updated.loots; 
          row.lootItems = updated.lootItems;
          if (this.details && this.details.id === row.id) {
            this.details.loots = updated.loots;
            this.details.lootItems = updated.lootItems;
          }
          this.closeModal(); 
          if (this.details) { this.detailsOpen = true; } 
        },
        error: (e) => console.error('Failed to add loot', e)
      });
    } else {
      this.bossApi.addAttendee(row.id, text).subscribe({
        next: (updated) => { row.attendees = updated.attendees; if (this.details && this.details.id === row.id) this.details.attendees = updated.attendees; this.closeModal(); if (this.details) { this.detailsOpen = true; } },
        error: (e) => console.error('Failed to add attendee', e)
      });
    }
  }

  async onImageSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    this.ocrLoading = true;
    this.ocrSuggestions = [];
    this.ocrAddedCount = 0;
    try {
      // Prefer server-side Vision AI extraction if configured
      try {
        const ai = await firstValueFrom(this.bossApi.extractFromImage(file, this.modalMode));
        if (this.modalMode === 'loot') {
          this.ocrSuggestions = ai.loots || [];
          if (this.activeRow && this.ocrSuggestions.length) await this.addSuggestedLoots(this.activeRow, this.ocrSuggestions);
        } else {
          this.ocrSuggestions = ai.attendees || [];
          if (this.activeRow && this.ocrSuggestions.length) await this.addSuggestedAttendees(this.activeRow, this.ocrSuggestions);
        }
      }
      catch {
        // Fallback to local OCR if server AI is not configured
        const { data } = await Tesseract.recognize(file, 'eng', { tessedit_char_blacklist: '.,;:!@#$%^*(){}<>|~`\"' });
        const text = (data.text || '').replace(/\r/g, '');
        if (this.modalMode === 'loot') {
          this.ocrSuggestions = this.parseLootFromText(text);
          if (this.activeRow && this.ocrSuggestions.length) await this.addSuggestedLoots(this.activeRow, this.ocrSuggestions);
        } else {
          this.ocrSuggestions = this.parseAttendeesFromText(text);
          if (this.activeRow && this.ocrSuggestions.length) await this.addSuggestedAttendees(this.activeRow, this.ocrSuggestions);
        }
      }
    } catch (e) {
      console.error('OCR failed', e);
    } finally {
      this.ocrLoading = false;
    }
  }

  private parseLootFromText(text: string): string[] {
    const candidates: string[] = [];
    // Normalize unicode quotes and similar, and split lines
    const cleaned = text.replace(/[“”]/g, '"').replace(/[’‘]/g, "'");
    const lines = cleaned.split(/\n+/).map(l => l.trim()).filter(Boolean);
    const lootRegex = /(acquired|obtained|acquire|loot(ed)?|received)\s+(.+)/i;
    for (const line of lines) {
      const m = line.match(lootRegex);
      if (m) {
        let item = m[3];
        // Remove trailing phrases like 'from ...'
        item = item.replace(/\s+from\s+.+$/i, '').trim();
        // Remove timestamps like [22:39]
        item = item.replace(/\[[0-9:]+\]\s*/g, '').trim();
        // Collapse multiple spaces
        item = item.replace(/\s{2,}/g, ' ');
        if (item && !candidates.includes(item)) candidates.push(item);
      }
    }
    return candidates.slice(0, 20);
  }

  private parseAttendeesFromText(text: string): string[] {
    const names = new Set<string>();
    const lines = text.split(/\n+/).map(l => l.trim()).filter(Boolean);
    for (let raw of lines) {
      // Strip timestamp like [22:39]
      let line = raw.replace(/^\s*\[[0-9:]+\]\s*/i, '');
      // Normalize spaces
      line = line.replace(/\s{2,}/g, ' ').trim();

      // Match: <name> acquired|obtained|received|looted ...
      const m = line.match(/^(\S+)\s+(acquired|obtained|received|looted|gets|got)\b/i);
      if (m) {
        const name = (m[1] || '').replace(/[^A-Za-z0-9_\-]/g, '').trim();
        if (name && !/^(acquired|obtained|received|looted|gets|got)$/i.test(name)) {
          names.add(name);
          continue;
        }
      }

      // Fallback: sometimes OCR merges with closing bracket like "]legendman" already handled by strip above
      // Another fallback: leaderboard/party grid where each cell contains only a name
      // Accept single-token lines that are not pure numbers and reasonable length
      const single = line.match(/^[^\s]+$/);
      if (single) {
        const token = single[0].replace(/[^A-Za-z0-9_\-]/g, '');
        if (token && !/^\d+$/.test(token) && token.length >= 2 && token.length <= 20) {
          names.add(token);
        }
      }
    }
    return Array.from(names).slice(0, 50);
  }

  private async addSuggestedLoots(row: BossDefeatDto, items: string[]): Promise<void> {
    // De-duplicate against current list (case-insensitive)
    const existing = new Set((row.loots || []).map(x => x.toLowerCase().trim()));
    const toAdd = items.filter(x => x && !existing.has(x.toLowerCase().trim()));
    let lastUpdated: BossDefeatDto | null = null;
    for (const item of toAdd) {
      try {
        const updated = await firstValueFrom(this.bossApi.addLoot(row.id, item));
        row.loots = updated.loots;
        row.lootItems = updated.lootItems;
        if (this.details && this.details.id === row.id) {
          this.details.loots = updated.loots;
          this.details.lootItems = updated.lootItems;
        }
        lastUpdated = updated;
        this.ocrAddedCount++;
      } catch (e) {
        console.error('Add loot failed', e);
      }
    }
    // If we started from details, reopen details view with freshest data
    if (lastUpdated && this.details && this.details.id === row.id) {
      this.details = await firstValueFrom(this.bossApi.historyById(row.id));
    }
  }

  private async addSuggestedAttendees(row: BossDefeatDto, items: string[]): Promise<void> {
    const existing = new Set((row.attendees || []).map(x => x.toLowerCase().trim()));
    const toAdd = items.filter(x => x && !existing.has(x.toLowerCase().trim()));
    let lastUpdated: BossDefeatDto | null = null;
    for (const item of toAdd) {
      try {
        const updated = await firstValueFrom(this.bossApi.addAttendee(row.id, item));
        row.attendees = updated.attendees;
        if (this.details && this.details.id === row.id) this.details.attendees = updated.attendees;
        lastUpdated = updated;
        this.ocrAddedCount++;
      } catch (e) {
        console.error('Add attendee failed', e);
      }
    }
    if (lastUpdated && this.details && this.details.id === row.id) {
      this.details = await firstValueFrom(this.bossApi.historyById(row.id));
    }
  }



  removeLoot(row: BossDefeatDto, index: number): void {
    this.bossApi.removeLoot(row.id, index).subscribe({
      next: (updated) => {
        // Update details view
        if (this.details && this.details.id === row.id) {
          this.details.loots = updated.loots;
          this.details.lootItems = updated.lootItems;
        }
        // Update list row
        const listRow = this.rows.find(r => r.id === row.id);
        if (listRow) {
          listRow.loots = updated.loots;
          listRow.lootItems = updated.lootItems;
        }
      },
      error: (e) => console.error('Failed to remove loot', e)
    });
  }

  removeAttendee(row: BossDefeatDto, index: number): void {
    this.bossApi.removeAttendee(row.id, index).subscribe({
      next: (updated) => {
        if (this.details && this.details.id === row.id) this.details.attendees = updated.attendees;
        const listRow = this.rows.find(r => r.id === row.id);
        if (listRow) listRow.attendees = updated.attendees;
      },
      error: (e) => console.error('Failed to remove attendee', e)
    });
  }

  updateLootPrice(row: BossDefeatDto, index: number, price: number | null): void {
    this.bossApi.updateLootPrice(row.id, index, price).subscribe({
      next: (updated) => {
        if (this.details && this.details.id === row.id) {
          this.details.lootItems = updated.lootItems;
          this.details.loots = updated.loots;
        }
        const listRow = this.rows.find(r => r.id === row.id);
        if (listRow) {
          listRow.lootItems = updated.lootItems;
          listRow.loots = updated.loots;
        }
      },
      error: (e) => console.error('Failed to update loot price', e)
    });
  }

  deleteHistory(row: BossDefeatDto): void {
    if (confirm(`Are you sure you want to delete this history record for ${row.bossName}?`)) {
      this.bossApi.deleteHistory(row.id).subscribe({
        next: () => {
          // Remove from the list
          this.rows = this.rows.filter(r => r.id !== row.id);
          // Close details if it was the deleted record
          if (this.details && this.details.id === row.id) {
            this.closeDetails();
          }
        },
        error: (e) => console.error('Failed to delete history record', e)
      });
    }
  }

  getTotalLootValue(details: BossDefeatDto): number {
    if (!details.lootItems || details.lootItems.length === 0) {
      return 0;
    }
    return details.lootItems.reduce((total, item) => {
      return total + (item.price || 0);
    }, 0);
  }

  onPriceChange(event: Event, details: BossDefeatDto, index: number): void {
    const target = event.target as HTMLInputElement;
    const value = target.value;
    const price = value ? Number(value) : null;
    this.updateLootPrice(details, index, price);
  }

  loadMembers(): void {
    this.loading = true;
    
    this.bossApi.getMembers().subscribe({
      next: (members) => {
        this.members = members;
        this.loading = false;
      },
      error: (e) => {
        console.error('Failed to load members', e);
        this.loading = false;
      }
    });
  }
}


