import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HistoryComponent } from './history/history.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { MembersComponent } from './members/members.component';
import { JaeComponent } from './jae/jae.component';
import { DashboardComponent } from './dashboard/dashboard.component';

const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' }, // Redirect root to dashboard
  { path: 'dashboard', component: DashboardComponent }, // Dashboard route with component
  { path: 'history', component: HistoryComponent },
  { path: 'notifications', component: NotificationsComponent },
  { path: 'members', component: MembersComponent },
  { path: 'jae', component: JaeComponent },
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
