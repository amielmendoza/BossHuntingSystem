import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    // Check if user is logged in
    if (!this.authService.isLoggedIn()) {
      return this.router.createUrlTree(['/login'], {
        queryParams: { returnUrl: state.url }
      });
    }

    // Check license status
    const licenseStatus = this.authService.getLicenseStatus();
    if (licenseStatus?.isExpired && !licenseStatus?.isInGracePeriod) {
      return this.router.createUrlTree(['/license-expired']);
    }

    // Check for required roles
    const requiredRoles = route.data['roles'] as string[];
    if (requiredRoles) {
      const user = this.authService.getCurrentUser();
      if (!user || !requiredRoles.includes(user.role)) {
        return this.router.createUrlTree(['/dashboard']);
      }
    }

    return true;
  }
}
