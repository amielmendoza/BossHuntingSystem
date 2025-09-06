import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HistoryComponent } from './history/history.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { MembersComponent } from './members/members.component';
import { JaeComponent } from './jae/jae.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { AdminAccessGuard } from './guards/admin-access.guard';

const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'history', component: HistoryComponent, canActivate: [AdminAccessGuard] },
  { path: 'notifications', component: NotificationsComponent, canActivate: [AdminAccessGuard] },
  { path: 'members', component: MembersComponent, canActivate: [AdminAccessGuard] },
  { path: 'jae', component: JaeComponent, canActivate: [AdminAccessGuard] },
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
