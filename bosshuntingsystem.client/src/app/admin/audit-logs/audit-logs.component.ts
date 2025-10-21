import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

interface AuditLog {
  id: number;
  clientId: number;
  clientName?: string;
  username: string;
  action: string;
  entityType: string;
  entityId: number | null;
  oldValues: string | null;
  newValues: string | null;
  ipAddress: string;
  timestamp: string;
}

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-logs.component.html',
  styleUrls: ['./audit-logs.component.css']
})
export class AuditLogsComponent implements OnInit {
  logs: AuditLog[] = [];
  filteredLogs: AuditLog[] = [];
  loading = false;
  error = '';
  Math = Math;

  // Filters
  filterClientId: number | null = null;
  filterUsername = '';
  filterAction = '';
  filterEntityType = '';
  filterStartDate = '';
  filterEndDate = '';

  // Pagination
  currentPage = 1;
  pageSize = 50;
  totalLogs = 0;

  // Available options
  actions = ['CREATE', 'UPDATE', 'DELETE', 'LOGIN', 'LOGOUT', 'PASSWORD_CHANGE'];
  entityTypes = ['Boss', 'Member', 'BossDefeat', 'User', 'Client'];

  // Selected log for details modal
  selectedLog: AuditLog | null = null;
  showDetailsModal = false;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.loading = true;
    this.error = '';

    const params: any = {
      page: this.currentPage,
      pageSize: this.pageSize
    };

    if (this.filterClientId) params.clientId = this.filterClientId;
    if (this.filterUsername) params.username = this.filterUsername;
    if (this.filterAction) params.action = this.filterAction;
    if (this.filterEntityType) params.entityType = this.filterEntityType;
    if (this.filterStartDate) params.startDate = this.filterStartDate;
    if (this.filterEndDate) params.endDate = this.filterEndDate;

    this.http.get<{ logs: AuditLog[], totalCount: number }>('/api/clients/audit-logs', { params })
      .subscribe({
        next: (response) => {
          this.logs = response.logs;
          this.filteredLogs = response.logs;
          this.totalLogs = response.totalCount;
          this.loading = false;
        },
        error: (error) => {
          this.error = error.error?.message || 'Failed to load audit logs';
          this.loading = false;
        }
      });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadLogs();
  }

  clearFilters(): void {
    this.filterClientId = null;
    this.filterUsername = '';
    this.filterAction = '';
    this.filterEntityType = '';
    this.filterStartDate = '';
    this.filterEndDate = '';
    this.currentPage = 1;
    this.loadLogs();
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadLogs();
    }
  }

  nextPage(): void {
    const maxPage = Math.ceil(this.totalLogs / this.pageSize);
    if (this.currentPage < maxPage) {
      this.currentPage++;
      this.loadLogs();
    }
  }

  getActionClass(action: string): string {
    switch (action) {
      case 'CREATE': return 'action-create';
      case 'UPDATE': return 'action-update';
      case 'DELETE': return 'action-delete';
      case 'LOGIN': return 'action-login';
      case 'LOGOUT': return 'action-logout';
      case 'PASSWORD_CHANGE': return 'action-password';
      default: return 'action-other';
    }
  }

  getActionIcon(action: string): string {
    switch (action) {
      case 'CREATE': return 'bi-plus-circle';
      case 'UPDATE': return 'bi-pencil-square';
      case 'DELETE': return 'bi-trash';
      case 'LOGIN': return 'bi-box-arrow-in-right';
      case 'LOGOUT': return 'bi-box-arrow-left';
      case 'PASSWORD_CHANGE': return 'bi-key';
      default: return 'bi-activity';
    }
  }

  showDetails(log: AuditLog): void {
    this.selectedLog = log;
    this.showDetailsModal = true;
  }

  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedLog = null;
  }

  formatJson(json: string | null): any {
    if (!json) return null;
    try {
      return JSON.parse(json);
    } catch {
      return json;
    }
  }

  exportToExcel(): void {
    this.loading = true;
    this.error = '';

    const params: any = {};
    if (this.filterClientId) params.clientId = this.filterClientId;
    if (this.filterUsername) params.username = this.filterUsername;
    if (this.filterAction) params.action = this.filterAction;
    if (this.filterEntityType) params.entityType = this.filterEntityType;
    if (this.filterStartDate) params.startDate = this.filterStartDate;
    if (this.filterEndDate) params.endDate = this.filterEndDate;

    this.http.get('/api/clients/audit-logs/export', { params, responseType: 'blob' })
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `audit_logs_${new Date().toISOString().split('T')[0]}.xlsx`;
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          window.URL.revokeObjectURL(url);
          this.loading = false;
        },
        error: (error) => {
          this.error = 'Failed to export audit logs';
          this.loading = false;
        }
      });
  }

  getTotalPages(): number {
    return Math.ceil(this.totalLogs / this.pageSize);
  }
}
