import { Injectable, Injector } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
  HttpResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private router: Router,
    private injector: Injector
  ) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Add JWT token to requests - read directly from localStorage to avoid circular dependency
    const token = localStorage.getItem('auth_token');
    console.log('[Interceptor] Token from localStorage:', token ? `${token.substring(0, 20)}...` : 'NO TOKEN');

    if (token) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      console.log('[Interceptor] Added Authorization header to:', request.url);
    } else {
      console.log('[Interceptor] No token available for:', request.url);
    }

    return next.handle(request).pipe(
      tap(event => {
        // Check for license status in response headers
        if (event instanceof HttpResponse) {
          const daysRemaining = event.headers.get('X-License-Days-Remaining');
          const inGracePeriod = event.headers.get('X-License-In-Grace-Period');

          if (daysRemaining || inGracePeriod) {
            // License info available in headers - could trigger warnings
            const days = parseInt(daysRemaining || '0', 10);
            const isGracePeriod = inGracePeriod === 'true';

            if (days <= 7 || isGracePeriod) {
              // Refresh license status to show warning - use injector to get AuthService lazily
              const authService = this.injector.get(AuthService);
              authService.refreshLicenseStatus();
            }
          }
        }
      }),
      catchError((error: HttpErrorResponse) => {
        // Don't intercept login/register endpoints - let them handle their own errors
        const isAuthEndpoint = request.url.includes('/api/auth/login') ||
                               request.url.includes('/api/auth/register');

        if (error.status === 401 && !isAuthEndpoint) {
          // Unauthorized - token expired or invalid (but not a login failure)
          console.error('[Interceptor] 401 Unauthorized for:', request.url);
          console.error('[Interceptor] Error details:', error);

          // Clear tokens and redirect to login
          localStorage.removeItem('auth_token');
          localStorage.removeItem('user_info');
          this.router.navigate(['/login']);
          return throwError(() => new Error('Session expired. Please login again.'));
        }

        if (error.status === 402) {
          // Payment Required - license expired
          this.router.navigate(['/license-expired']);
          return throwError(() => new Error('License expired. Please contact support.'));
        }

        if (error.status === 403) {
          // Forbidden - insufficient permissions
          return throwError(() => new Error('You do not have permission to perform this action.'));
        }

        return throwError(() => error);
      })
    );
  }
}
