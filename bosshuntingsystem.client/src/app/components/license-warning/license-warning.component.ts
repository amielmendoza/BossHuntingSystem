import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { AuthService, LicenseStatus } from '../../services/auth.service';

@Component({
  selector: 'app-license-warning',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './license-warning.component.html',
  styleUrls: ['./license-warning.component.css']
})
export class LicenseWarningComponent implements OnInit, OnDestroy {
  licenseStatus: LicenseStatus | null = null;
  showWarning = false;
  private subscription?: Subscription;

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.subscription = this.authService.licenseStatus$.subscribe(status => {
      this.licenseStatus = status;
      this.updateWarningVisibility();
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  private updateWarningVisibility(): void {
    if (!this.licenseStatus) {
      this.showWarning = false;
      return;
    }

    // Show warning if:
    // 1. In grace period (expired but still accessible)
    // 2. Less than 7 days remaining
    this.showWarning =
      this.licenseStatus.isInGracePeriod ||
      (this.licenseStatus.daysRemaining > 0 && this.licenseStatus.daysRemaining <= 7);
  }

  getAlertClass(): string {
    if (!this.licenseStatus) return 'alert-info';

    if (this.licenseStatus.isInGracePeriod) {
      return 'alert-danger';
    } else if (this.licenseStatus.daysRemaining <= 3) {
      return 'alert-danger';
    } else if (this.licenseStatus.daysRemaining <= 7) {
      return 'alert-warning';
    }

    return 'alert-info';
  }

  getIcon(): string {
    if (!this.licenseStatus) return 'bi-info-circle-fill';

    if (this.licenseStatus.isInGracePeriod || this.licenseStatus.daysRemaining <= 3) {
      return 'bi-exclamation-triangle-fill';
    }

    return 'bi-clock-fill';
  }

  dismissWarning(): void {
    this.showWarning = false;
  }
}
