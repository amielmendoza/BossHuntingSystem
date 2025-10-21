import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

interface Client {
  id: number;
  name: string;
  licenseKey: string;
  licenseExpirationDate: string;
  isActive: boolean;
  contactEmail: string;
  gracePeriodDays: number;
  createdDate: string;
  daysUntilExpiration?: number;
  isExpired?: boolean;
}

interface CreateClientRequest {
  name: string;
  contactEmail: string;
  licenseExpirationDate: string;
  gracePeriodDays: number;
}

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './clients.component.html',
  styleUrls: ['./clients.component.css']
})
export class ClientsComponent implements OnInit {
  clients: Client[] = [];
  loading = false;
  error = '';

  // Modal state
  showCreateModal = false;
  showRenewModal = false;
  selectedClient: Client | null = null;

  // Create form
  newClient: CreateClientRequest = {
    name: '',
    contactEmail: '',
    licenseExpirationDate: '',
    gracePeriodDays: 7
  };

  // Renew form
  renewMonths = 12;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadClients();
  }

  loadClients(): void {
    this.loading = true;
    this.error = '';

    this.http.get<Client[]>('/api/clients').subscribe({
      next: (clients) => {
        this.clients = clients.map(client => ({
          ...client,
          daysUntilExpiration: this.calculateDaysUntilExpiration(client.licenseExpirationDate),
          isExpired: new Date(client.licenseExpirationDate) < new Date()
        }));
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || 'Failed to load clients';
        this.loading = false;
      }
    });
  }

  calculateDaysUntilExpiration(expirationDate: string): number {
    const expiry = new Date(expirationDate);
    const today = new Date();
    const diffTime = expiry.getTime() - today.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  getStatusClass(client: Client): string {
    if (!client.isActive) return 'status-inactive';
    if (client.isExpired) return 'status-expired';
    if (client.daysUntilExpiration && client.daysUntilExpiration <= 7) return 'status-warning';
    return 'status-active';
  }

  getStatusText(client: Client): string {
    if (!client.isActive) return 'Inactive';
    if (client.isExpired) return 'Expired';
    if (client.daysUntilExpiration && client.daysUntilExpiration <= 7) return `Expires in ${client.daysUntilExpiration} days`;
    return 'Active';
  }

  openCreateModal(): void {
    this.newClient = {
      name: '',
      contactEmail: '',
      licenseExpirationDate: this.getDefaultExpirationDate(),
      gracePeriodDays: 7
    };
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  getDefaultExpirationDate(): string {
    const date = new Date();
    date.setFullYear(date.getFullYear() + 1);
    return date.toISOString().split('T')[0];
  }

  createClient(): void {
    if (!this.newClient.name || !this.newClient.contactEmail) {
      this.error = 'Please fill in all required fields';
      return;
    }

    this.loading = true;
    this.error = '';

    this.http.post<Client>('/api/clients', this.newClient).subscribe({
      next: (client) => {
        this.loadClients();
        this.closeCreateModal();
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || 'Failed to create client';
        this.loading = false;
      }
    });
  }

  openRenewModal(client: Client): void {
    this.selectedClient = client;
    this.renewMonths = 12;
    this.showRenewModal = true;
  }

  closeRenewModal(): void {
    this.showRenewModal = false;
    this.selectedClient = null;
  }

  renewLicense(): void {
    if (!this.selectedClient) return;

    this.loading = true;
    this.error = '';

    this.http.post(`/api/clients/${this.selectedClient.id}/renew`, { months: this.renewMonths })
      .subscribe({
        next: () => {
          this.loadClients();
          this.closeRenewModal();
          this.loading = false;
        },
        error: (error) => {
          this.error = error.error?.message || 'Failed to renew license';
          this.loading = false;
        }
      });
  }

  toggleActive(client: Client): void {
    const action = client.isActive ? 'deactivate' : 'activate';

    if (!confirm(`Are you sure you want to ${action} ${client.name}?`)) {
      return;
    }

    this.loading = true;
    this.error = '';

    this.http.post(`/api/clients/${client.id}/${action}`, {}).subscribe({
      next: () => {
        this.loadClients();
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || `Failed to ${action} client`;
        this.loading = false;
      }
    });
  }

  exportToExcel(): void {
    this.loading = true;
    this.error = '';

    this.http.get('/api/clients/export', { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `clients_${new Date().toISOString().split('T')[0]}.xlsx`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.loading = false;
      },
      error: (error) => {
        this.error = 'Failed to export clients';
        this.loading = false;
      }
    });
  }

  copyLicenseKey(licenseKey: string): void {
    navigator.clipboard.writeText(licenseKey).then(() => {
      // Could show a toast notification here
      console.log('License key copied to clipboard');
    });
  }

  calculateNewExpirationDate(currentExpiration: string, months: number): Date {
    const date = new Date(currentExpiration);
    date.setMonth(date.getMonth() + months);
    return date;
  }
}
