import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { Observable, map, take } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(): Observable<boolean> {
    return this.authService.isCheckingAuth$.pipe(
      take(1),
      map(isChecking => {
        if (isChecking) {
          // Still checking authentication, wait for result
          return false;
        }
        
        if (this.authService.isAuthenticated()) {
          return true;
        }

        // Not authenticated, redirect to login
        console.log('[AuthGuard] Access denied - redirecting to login');
        this.router.navigate(['/login']);
        return false;
      })
    );
  }
}
