import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService, LicenseStatus } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-license-expired',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './license-expired.component.html',
  styleUrls: ['./license-expired.component.css']
})
export class LicenseExpiredComponent implements OnInit {
  licenseStatus: LicenseStatus | null = null;
  clientName = '';

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    this.clientName = user?.clientName || 'Your organization';

    // Get license status
    this.licenseStatus = this.authService.getLicenseStatus();

    // If license is actually valid, redirect to dashboard
    if (this.licenseStatus?.isValid && !this.licenseStatus?.isExpired) {
      this.router.navigate(['/']);
    }
  }

  logout(): void {
    this.authService.logout();
  }
}
