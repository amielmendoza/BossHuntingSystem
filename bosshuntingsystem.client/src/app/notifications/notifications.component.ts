import { Component } from '@angular/core';
import { BossService } from '../boss.service';

@Component({
  selector: 'app-notifications',
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.css']
})
export class NotificationsComponent {
  message: string = '';
  isSending: boolean = false;
  lastResult: { success: boolean; message: string } | null = null;

  constructor(private bossApi: BossService) {}

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
