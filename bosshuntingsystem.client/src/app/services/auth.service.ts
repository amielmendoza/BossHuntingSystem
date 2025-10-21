import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  email: string;
  role: string;
  clientId: number;
  clientName: string;
}

export interface UserInfo {
  id: number;
  username: string;
  email: string;
  role: string;
  clientId: number;
  clientName: string;
  isActive: boolean;
  lastLoginDate?: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface LicenseStatus {
  isValid: boolean;
  isExpired: boolean;
  isInGracePeriod: boolean;
  daysRemaining: number;
  expirationDate: string;
  gracePeriodEnd?: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'user_info';
  private currentUserSubject = new BehaviorSubject<LoginResponse | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();

  private licenseStatusSubject = new BehaviorSubject<LicenseStatus | null>(null);
  public licenseStatus$ = this.licenseStatusSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // Load license status on startup if logged in
    if (this.isLoggedIn()) {
      this.refreshLicenseStatus();
    }
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', credentials).pipe(
      tap(response => {
        this.setSession(response);
        this.refreshLicenseStatus();
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSubject.next(null);
    this.licenseStatusSubject.next(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;

    // Check if token is expired (simple check - decode JWT)
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expiry = payload.exp * 1000; // Convert to milliseconds
      return Date.now() < expiry;
    } catch {
      return false;
    }
  }

  getCurrentUser(): LoginResponse | null {
    return this.currentUserSubject.value;
  }

  getUserInfo(): Observable<UserInfo> {
    return this.http.get<UserInfo>('/api/auth/me');
  }

  changePassword(request: ChangePasswordRequest): Observable<any> {
    return this.http.post('/api/auth/change-password', request);
  }

  refreshLicenseStatus(): void {
    const user = this.getCurrentUser();
    if (user) {
      this.http.get<LicenseStatus>('/api/clients/my-license').subscribe({
        next: (status) => this.licenseStatusSubject.next(status),
        error: () => this.licenseStatusSubject.next(null)
      });
    }
  }

  getLicenseStatus(): LicenseStatus | null {
    return this.licenseStatusSubject.value;
  }

  isSuperAdmin(): boolean {
    const user = this.getCurrentUser();
    return user?.role === 'SuperAdmin';
  }

  isAdmin(): boolean {
    const user = this.getCurrentUser();
    return user?.role === 'Admin' || user?.role === 'SuperAdmin';
  }

  hasRole(role: string): boolean {
    const user = this.getCurrentUser();
    return user?.role === role;
  }

  private setSession(authResult: LoginResponse): void {
    localStorage.setItem(this.TOKEN_KEY, authResult.token);
    localStorage.setItem(this.USER_KEY, JSON.stringify(authResult));
    this.currentUserSubject.next(authResult);
  }

  private getUserFromStorage(): LoginResponse | null {
    const userStr = localStorage.getItem(this.USER_KEY);
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch {
        return null;
      }
    }
    return null;
  }
}
