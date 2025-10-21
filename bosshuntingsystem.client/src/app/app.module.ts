import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HistoryComponent } from './history/history.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { MembersComponent } from './members/members.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { PointsComponent } from './points/points.component';
import { CacheInterceptor } from './cache-interceptor';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { JaeComponent } from './jae/jae.component';
import { DateUtilsService } from './utils/date-utils.service';
import { MemberDeletionComponent } from './member-deletion/member-deletion.component';
import { LoginComponent } from './login/login.component';
import { LicenseWarningComponent } from './components/license-warning/license-warning.component';
import { LicenseExpiredComponent } from './pages/license-expired/license-expired.component';
import { ClientsComponent } from './admin/clients/clients.component';
import { AuditLogsComponent } from './admin/audit-logs/audit-logs.component';
import { UsersComponent } from './admin/users/users.component';

@NgModule({
  declarations: [
    AppComponent,
    HistoryComponent,
    NotificationsComponent,
    MembersComponent,
    DashboardComponent,
    PointsComponent,
    JaeComponent,
    MemberDeletionComponent
  ],
  imports: [
    BrowserModule,
    CommonModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    AppRoutingModule,
    NgbModule,
    // Standalone components
    LoginComponent,
    LicenseWarningComponent,
    LicenseExpiredComponent,
    ClientsComponent,
    AuditLogsComponent,
    UsersComponent
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: CacheInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    },
    DateUtilsService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
