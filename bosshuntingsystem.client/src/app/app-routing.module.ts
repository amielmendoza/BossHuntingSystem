import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HistoryComponent } from './history/history.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { MembersComponent } from './members/members.component';
import { PointsComponent } from './points/points.component';
import { JaeComponent } from './jae/jae.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { MemberDeletionComponent } from './member-deletion/member-deletion.component';
import { LoginComponent } from './login/login.component';
import { LicenseExpiredComponent } from './pages/license-expired/license-expired.component';
import { ClientsComponent } from './admin/clients/clients.component';
import { AuditLogsComponent } from './admin/audit-logs/audit-logs.component';
import { UsersComponent } from './admin/users/users.component';
import { AdminAccessGuard } from './guards/admin-access.guard';
import { AuthGuard } from './guards/auth.guard';

const routes: Routes = [
  // Public routes
  { path: 'login', component: LoginComponent },
  { path: 'license-expired', component: LicenseExpiredComponent },

  // Protected routes
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },
  { path: 'history', component: HistoryComponent, canActivate: [AuthGuard, AdminAccessGuard] },
  { path: 'notifications', component: NotificationsComponent, canActivate: [AuthGuard, AdminAccessGuard] },
  { path: 'members', component: MembersComponent, canActivate: [AuthGuard, AdminAccessGuard] },
  { path: 'member-deletion', component: MemberDeletionComponent, canActivate: [AuthGuard, AdminAccessGuard] },
  { path: 'points', component: PointsComponent, canActivate: [AuthGuard, AdminAccessGuard] },
  { path: 'jae', component: JaeComponent, canActivate: [AuthGuard, AdminAccessGuard] },

  // Admin routes (SuperAdmin only)
  {
    path: 'admin',
    canActivate: [AuthGuard],
    children: [
      { path: 'clients', component: ClientsComponent, data: { roles: ['SuperAdmin'] } },
      { path: 'audit-logs', component: AuditLogsComponent, data: { roles: ['SuperAdmin', 'Admin'] } },
      { path: 'users', component: UsersComponent, data: { roles: ['Manager', 'Admin', 'SuperAdmin'] } },
      { path: '', redirectTo: 'clients', pathMatch: 'full' }
    ]
  },

  // Fallback
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
