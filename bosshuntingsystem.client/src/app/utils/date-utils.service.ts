import { Injectable } from '@angular/core';

@Injectable()
export class DateUtilsService {

  /**
   * Converts a date from the server to a local Date object
   * The server now sends dates already converted to PHT, so we just create a Date object
   * @param serverDateInput - Date string from server (already in PHT) or Date object
   * @returns Date object representing PHT time
   */
  utcToLocal(serverDateInput: string | Date | null): Date | null {
    if (!serverDateInput) return null;
    
    // If it's already a Date object, return as-is
    if (serverDateInput instanceof Date) {
      return serverDateInput;
    }
    
    // Server sends dates in PHT format, so just parse them
    return new Date(serverDateInput);
  }

  /**
   * Converts a local PHT Date to UTC ISO string for server communication
   * @param phtDate - PHT Date object
   * @returns UTC ISO string
   */
  localToUtc(phtDate: Date): string {
    // Create a new date assuming the input represents PHT time
    // We need to convert it to UTC for server storage
    const phtYear = phtDate.getFullYear();
    const phtMonth = phtDate.getMonth();
    const phtDate_day = phtDate.getDate();
    const phtHours = phtDate.getHours();
    const phtMinutes = phtDate.getMinutes();
    const phtSeconds = phtDate.getSeconds();
    const phtMilliseconds = phtDate.getMilliseconds();
    
    // Create UTC date by subtracting 8 hours from the PHT time
    const utcDate = new Date(Date.UTC(phtYear, phtMonth, phtDate_day, phtHours - 8, phtMinutes, phtSeconds, phtMilliseconds));
    
    return utcDate.toISOString();
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
   * Converts server date (already in PHT) to PHT formatted string for display
   * @param serverDateInput - Date string from server (already in PHT) or Date object
   * @returns Formatted PHT date string
   */
  formatUtcForDisplay(serverDateInput: string | Date | null): string {
    if (!serverDateInput) return '';
    
    // If it's already a Date object, format directly
    if (serverDateInput instanceof Date) {
      return this.formatForDisplay(serverDateInput);
    }
    
    // Server sends dates in PHT format, so just parse and format
    const date = new Date(serverDateInput);
    return this.formatForDisplay(date);
  }

  /**
   * Gets current time in PHT
   * @returns Current PHT Date object
   */
  getCurrentPhtTime(): Date {
    // Get current time in PHT timezone
    const now = new Date();
    const phtTime = new Date(now.toLocaleString("en-US", { timeZone: "Asia/Manila" }));
    return phtTime;
  }

  /**
   * Converts a datetime-local input string to PHT Date
   * @param datetimeLocalString - String from datetime-local input (assumed to be in PHT)
   * @returns PHT Date object
   */
  datetimeLocalToPht(datetimeLocalString: string): Date {
    // The datetime-local input is assumed to be in PHT
    // Create a Date object treating it as local time (PHT)
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
    
    // Use Asia/Manila timezone for consistent PHT display
    return phtDate.toLocaleString('en-US', {
      timeZone: 'Asia/Manila',
      month: '2-digit',
      day: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  }
}
