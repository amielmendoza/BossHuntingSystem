import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError, of } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  message: string;
  token: string;
  username: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/api/auth`;
  private readonly tokenKey = 'auth_token';
  private readonly usernameKey = 'auth_username';
  
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  private currentUserSubject = new BehaviorSubject<string | null>(null);
  private isCheckingAuthSubject = new BehaviorSubject<boolean>(true);

  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  public currentUser$ = this.currentUserSubject.asObservable();
  public isCheckingAuth$ = this.isCheckingAuthSubject.asObservable();

  constructor(private http: HttpClient) {
    this.checkAuthStatus();
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials)
      .pipe(
        tap(response => {
          if (response.success) {
            this.setAuthData(response.token, response.username);
          }
        })
      );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.usernameKey);
    this.isAuthenticatedSubject.next(false);
    this.currentUserSubject.next(null);
  }

  // Force logout all users by clearing all authentication data
  forceLogoutAllUsers(): void {
    // Clear all authentication-related data from localStorage
    const keysToRemove = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && (key.startsWith('auth_') || key.includes('token') || key.includes('user'))) {
        keysToRemove.push(key);
      }
    }
    
    keysToRemove.forEach(key => localStorage.removeItem(key));
    
    // Reset authentication state
    this.isAuthenticatedSubject.next(false);
    this.currentUserSubject.next(null);
    
    console.log('[AuthService] Forced logout of all users - cleared authentication data');
  }

  isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getCurrentUser(): string | null {
    return this.currentUserSubject.value;
  }

  private setAuthData(token: string, username: string): void {
    localStorage.setItem(this.tokenKey, token);
    localStorage.setItem(this.usernameKey, username);
    this.isAuthenticatedSubject.next(true);
    this.currentUserSubject.next(username);
  }

  private checkAuthStatus(): void {
    const token = localStorage.getItem(this.tokenKey);
    const username = localStorage.getItem(this.usernameKey);
    
    if (!token || !username) {
      // No token or username found - immediately set as not authenticated
      this.isAuthenticatedSubject.next(false);
      this.currentUserSubject.next(null);
      this.isCheckingAuthSubject.next(false);
      return;
    }

    // Validate token with backend
    this.http.post<boolean>(`${this.apiUrl}/validate`, token).pipe(
      catchError(() => of(false))
    ).subscribe({
      next: (isValid) => {
        if (isValid) {
          this.isAuthenticatedSubject.next(true);
          this.currentUserSubject.next(username);
        } else {
          this.logout();
        }
        this.isCheckingAuthSubject.next(false);
      },
      error: () => {
        this.logout();
        this.isCheckingAuthSubject.next(false);
      }
    });
  }
}
