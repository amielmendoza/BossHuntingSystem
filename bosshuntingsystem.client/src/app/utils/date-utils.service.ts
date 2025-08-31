import { Injectable } from '@angular/core';

@Injectable()
export class DateUtilsService {

  // PHT timezone offset: GMT+8 (8 hours ahead of UTC)
  private readonly PHT_OFFSET_HOURS = 8;

  /**
   * Converts a UTC date string to PHT (Philippine Time) Date object
   * @param utcDateInput - ISO date string from server (UTC) or Date object
   * @returns PHT Date object
   */
  utcToLocal(utcDateInput: string | Date | null): Date | null {
    if (!utcDateInput) return null;
    
    // If it's already a Date object, assume it's already in PHT
    if (utcDateInput instanceof Date) {
      return utcDateInput;
    }
    
    // Create a Date object from the UTC string and convert to PHT
    const utcDate = new Date(utcDateInput);
    const phtDate = new Date(utcDate.getTime() + (this.PHT_OFFSET_HOURS * 60 * 60 * 1000));
    
    return phtDate;
  }

  /**
   * Converts a PHT Date to UTC string for server communication
   * @param phtDate - PHT Date object
   * @returns UTC ISO string
   */
  localToUtc(phtDate: Date): string {
    // Convert PHT to UTC by subtracting 8 hours
    const utcTime = new Date(phtDate.getTime() - (this.PHT_OFFSET_HOURS * 60 * 60 * 1000));
    
    return utcTime.toISOString();
  }

  /**
   * Formats a date for display in PHT format
   * @param date - Date object to format (assumed to be in PHT)
   * @returns Formatted date string in PHT
   */
  formatForDisplay(date: Date | null): string {
    if (!date) return '';
    
    return date.toLocaleString('en-US', {
      timeZone: 'Asia/Manila',
      month: '2-digit',
      day: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  }

  /**
   * Converts UTC string or Date to PHT formatted string for display
   * @param utcDateInput - UTC date string from server or Date object
   * @returns Formatted PHT date string
   */
  formatUtcForDisplay(utcDateInput: string | Date | null): string {
    if (!utcDateInput) return '';
    
    // If it's already a Date object, assume it's already in PHT
    if (utcDateInput instanceof Date) {
      return this.formatForDisplay(utcDateInput);
    }
    
    // For UTC strings, convert to PHT and format
    const utcDate = new Date(utcDateInput);
    const phtDate = new Date(utcDate.getTime() + (this.PHT_OFFSET_HOURS * 60 * 60 * 1000));
    
    return this.formatForDisplay(phtDate);
  }

  /**
   * Gets current time in PHT
   * @returns Current PHT Date object
   */
  getCurrentPhtTime(): Date {
    const utcNow = new Date();
    return new Date(utcNow.getTime() + (this.PHT_OFFSET_HOURS * 60 * 60 * 1000));
  }

  /**
   * Converts a datetime-local input string to PHT Date
   * @param datetimeLocalString - String from datetime-local input (assumed to be in PHT)
   * @returns PHT Date object
   */
  datetimeLocalToPht(datetimeLocalString: string): Date {
    // The datetime-local input is assumed to be in PHT
    // Create a Date object and ensure it's treated as PHT
    const phtDate = new Date(datetimeLocalString);
    return phtDate;
  }

  /**
   * Converts a PHT Date to datetime-local input string format
   * @param phtDate - PHT Date object
   * @returns String formatted for datetime-local input
   */
  phtToDatetimeLocal(phtDate: Date): string {
    // Format for datetime-local input: YYYY-MM-DDTHH:mm
    const year = phtDate.getFullYear();
    const month = String(phtDate.getMonth() + 1).padStart(2, '0');
    const day = String(phtDate.getDate()).padStart(2, '0');
    const hours = String(phtDate.getHours()).padStart(2, '0');
    const minutes = String(phtDate.getMinutes()).padStart(2, '0');
    
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  /**
   * Formats a PHT Date object for display (assumes date is already in PHT)
   * @param phtDate - PHT Date object
   * @returns Formatted PHT date string
   */
  formatPhtForDisplay(phtDate: Date | null): string {
    if (!phtDate) return '';
    
    return phtDate.toLocaleString('en-US', {
      month: '2-digit',
      day: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  }
}
