import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HistoryComponent } from './history/history.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { MembersComponent } from './members/members.component';
import { JaeComponent } from './jae/jae.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { LoginComponent } from './login/login.component';
import { AuthGuard } from './guards/auth.guard';
import { EmergencyAuthGuard } from './guards/emergency-auth.guard';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { 
    path: 'dashboard', 
    component: DashboardComponent, 
    canActivate: [EmergencyAuthGuard, AuthGuard] 
  },
  { 
    path: 'history', 
    component: HistoryComponent, 
    canActivate: [EmergencyAuthGuard, AuthGuard] 
  },
  { 
    path: 'notifications', 
    component: NotificationsComponent, 
    canActivate: [EmergencyAuthGuard, AuthGuard] 
  },
  { 
    path: 'members', 
    component: MembersComponent, 
    canActivate: [EmergencyAuthGuard, AuthGuard] 
  },
  { 
    path: 'jae', 
    component: JaeComponent, 
    canActivate: [EmergencyAuthGuard, AuthGuard] 
  },
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
