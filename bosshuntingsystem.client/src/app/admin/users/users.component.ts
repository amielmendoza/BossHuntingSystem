import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';

interface User {
  id: number;
  username: string;
  email: string;
  role: string;
  clientId: number;
  isActive: boolean;
  createdDate: string;
  lastLoginDate?: string;
}

interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  role: string;
}

interface UpdateUserRequest {
  email: string;
  role: string;
  isActive: boolean;
}

interface ChangePasswordRequest {
  newPassword: string;
}

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.css']
})
export class UsersComponent implements OnInit {
  users: User[] = [];
  loading = false;
  error = '';
  currentUser: any;

  // Modal state
  showCreateModal = false;
  showEditModal = false;
  showPasswordModal = false;
  selectedUser: User | null = null;

  // Create form
  newUser: CreateUserRequest = {
    username: '',
    email: '',
    password: '',
    role: 'User'
  };

  // Edit form
  editUser: UpdateUserRequest = {
    email: '',
    role: '',
    isActive: true
  };

  // Password form
  newPassword = '';
  confirmPassword = '';

  // Available roles
  roles = ['User', 'Manager', 'Admin'];

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getCurrentUser();
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.error = '';

    this.http.get<User[]>('/api/users').subscribe({
      next: (users) => {
        this.users = users;
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || 'Failed to load users';
        this.loading = false;
      }
    });
  }

  openCreateModal(): void {
    this.newUser = {
      username: '',
      email: '',
      password: '',
      role: 'User'
    };
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  createUser(): void {
    if (!this.newUser.username || !this.newUser.email || !this.newUser.password) {
      this.error = 'Please fill in all required fields';
      return;
    }

    if (this.newUser.password.length < 6) {
      this.error = 'Password must be at least 6 characters';
      return;
    }

    this.loading = true;
    this.error = '';

    this.http.post<User>('/api/users', this.newUser).subscribe({
      next: (user) => {
        this.loadUsers();
        this.closeCreateModal();
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || 'Failed to create user';
        this.loading = false;
      }
    });
  }

  openEditModal(user: User): void {
    this.selectedUser = user;
    this.editUser = {
      email: user.email,
      role: user.role,
      isActive: user.isActive
    };
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.selectedUser = null;
  }

  updateUser(): void {
    if (!this.selectedUser) return;

    if (!this.editUser.email) {
      this.error = 'Email is required';
      return;
    }

    this.loading = true;
    this.error = '';

    this.http.put(`/api/users/${this.selectedUser.id}`, this.editUser).subscribe({
      next: () => {
        this.loadUsers();
        this.closeEditModal();
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || 'Failed to update user';
        this.loading = false;
      }
    });
  }

  openPasswordModal(user: User): void {
    this.selectedUser = user;
    this.newPassword = '';
    this.confirmPassword = '';
    this.showPasswordModal = true;
  }

  closePasswordModal(): void {
    this.showPasswordModal = false;
    this.selectedUser = null;
    this.newPassword = '';
    this.confirmPassword = '';
  }

  changePassword(): void {
    if (!this.selectedUser) return;

    if (!this.newPassword || !this.confirmPassword) {
      this.error = 'Please fill in all password fields';
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.error = 'Passwords do not match';
      return;
    }

    if (this.newPassword.length < 6) {
      this.error = 'Password must be at least 6 characters';
      return;
    }

    this.loading = true;
    this.error = '';

    this.http.post(`/api/users/${this.selectedUser.id}/change-password`, { newPassword: this.newPassword })
      .subscribe({
        next: () => {
          this.closePasswordModal();
          this.loading = false;
          alert('Password changed successfully');
        },
        error: (error) => {
          this.error = error.error?.message || 'Failed to change password';
          this.loading = false;
        }
      });
  }

  deleteUser(user: User): void {
    if (user.id === this.currentUser?.id) {
      alert('You cannot delete your own account');
      return;
    }

    if (!confirm(`Are you sure you want to delete user "${user.username}"? This action cannot be undone.`)) {
      return;
    }

    this.loading = true;
    this.error = '';

    this.http.delete(`/api/users/${user.id}`).subscribe({
      next: () => {
        this.loadUsers();
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || 'Failed to delete user';
        this.loading = false;
      }
    });
  }

  toggleActive(user: User): void {
    if (user.id === this.currentUser?.id) {
      alert('You cannot deactivate your own account');
      return;
    }

    const action = user.isActive ? 'deactivate' : 'activate';

    if (!confirm(`Are you sure you want to ${action} user "${user.username}"?`)) {
      return;
    }

    this.editUser = {
      email: user.email,
      role: user.role,
      isActive: !user.isActive
    };

    this.loading = true;
    this.error = '';

    this.http.put(`/api/users/${user.id}`, this.editUser).subscribe({
      next: () => {
        this.loadUsers();
        this.loading = false;
      },
      error: (error) => {
        this.error = error.error?.message || `Failed to ${action} user`;
        this.loading = false;
      }
    });
  }

  getRoleBadgeClass(role: string): string {
    switch (role) {
      case 'SuperAdmin': return 'role-superadmin';
      case 'Admin': return 'role-admin';
      case 'Manager': return 'role-manager';
      case 'User': return 'role-user';
      default: return 'role-other';
    }
  }

  getStatusBadgeClass(isActive: boolean): string {
    return isActive ? 'status-active' : 'status-inactive';
  }
}
