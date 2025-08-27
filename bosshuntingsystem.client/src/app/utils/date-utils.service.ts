import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DateUtilsService {

  /**
   * Converts a UTC date string to a local Date object
   * @param utcDateInput - ISO date string from server (UTC) or Date object
   * @returns Local Date object
   */
  utcToLocal(utcDateInput: string | Date | null): Date | null {
    if (!utcDateInput) return null;
    
    // If it's already a Date object, assume it's already local
    if (utcDateInput instanceof Date) {
      return utcDateInput;
    }
    
    // Create a Date object from the UTC string
    const utcDate = new Date(utcDateInput);
    
    // Convert to local time by adjusting for timezone offset
    const localDate = new Date(utcDate.getTime() - (utcDate.getTimezoneOffset() * 60000));
    
    return localDate;
  }

  /**
   * Converts a local Date to UTC string for server communication
   * @param localDate - Local Date object
   * @returns UTC ISO string
   */
  localToUtc(localDate: Date): string {
    return localDate.toISOString();
  }

  /**
   * Formats a date for display in the consistent format used across the app
   * @param date - Date object to format
   * @returns Formatted date string
   */
  formatForDisplay(date: Date | null): string {
    if (!date) return '';
    
    return date.toLocaleString('en-US', {
      month: '2-digit',
      day: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  }

  /**
   * Converts UTC string or Date to local formatted string for display
   * @param utcDateInput - UTC date string from server or Date object
   * @returns Formatted local date string
   */
  formatUtcForDisplay(utcDateInput: string | Date | null): string {
    const localDate = this.utcToLocal(utcDateInput);
    return this.formatForDisplay(localDate);
  }
}
