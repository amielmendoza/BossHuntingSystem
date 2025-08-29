import { Component, OnInit } from '@angular/core';
import { BossService, IpRestrictionInfo } from '../boss.service';

@Component({
  selector: 'app-notifications',
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.css']
})
export class NotificationsComponent implements OnInit {
  message: string = '';
  isSending: boolean = false;
  lastResult: { success: boolean; message: string } | null = null;
  
  // IP restriction state
  ipRestrictionInfo: IpRestrictionInfo | null = null;
  isIpRestricted = false;

  constructor(private bossApi: BossService) {}

  ngOnInit(): void {
    // Check IP restrictions
    this.checkIpRestrictions();
  }

  checkIpRestrictions(): void {
    this.bossApi.checkIpRestrictions().subscribe({
      next: (info) => {
        this.ipRestrictionInfo = info;
        this.isIpRestricted = info.isRestricted;
        console.log('[Notifications] IP restriction check:', info);
        console.log('[Notifications] Client IP:', info.clientIp);
        console.log('[Notifications] Is Restricted:', info.isRestricted);
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

  sendNotification(): void {
    if (!this.message.trim()) {
      this.lastResult = { success: false, message: 'Please enter a message' };
      return;
    }

    this.isSending = true;
    this.lastResult = null;

    this.bossApi.sendManualNotification(this.message).subscribe({
      next: (response) => {
        this.isSending = false;
        this.lastResult = { success: true, message: 'Notification sent successfully!' };
        this.message = ''; // Clear the message after successful send
      },
      error: (error) => {
        this.isSending = false;
        this.lastResult = { success: false, message: 'Failed to send notification. Please try again.' };
        console.error('Error sending notification:', error);
      }
    });
  }

  clearResult(): void {
    this.lastResult = null;
  }
}
