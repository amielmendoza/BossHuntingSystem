import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable, map } from 'rxjs';
import { GlobalAuthService } from '../services/global-auth.service';

@Injectable({
  providedIn: 'root'
})
export class EmergencyAuthGuard implements CanActivate {
  constructor(
    private globalAuthService: GlobalAuthService,
    private router: Router
  ) {}

  canActivate(): Observable<boolean> {
    return this.globalAuthService.maintenanceMode$.pipe(
      map(isMaintenanceMode => {
        if (isMaintenanceMode) {
          console.log('[EmergencyAuthGuard] Access blocked - system in maintenance mode');
          this.router.navigate(['/login']);
          return false;
        }
        return true;
      })
    );
  }
}


