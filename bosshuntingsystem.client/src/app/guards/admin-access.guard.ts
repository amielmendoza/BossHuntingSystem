import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { AccessControlService } from '../services/access-control.service';

@Injectable({
  providedIn: 'root'
})
export class AdminAccessGuard implements CanActivate {

  constructor(
    private accessControl: AccessControlService,
    private router: Router
  ) {}

  canActivate(): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    if (this.accessControl.hasManagerAccess()) {
      return true;
    }

    // Redirect to dashboard if no manager access
    console.warn('Access denied: Redirecting to dashboard');
    return this.router.createUrlTree(['/dashboard']);
  }
}