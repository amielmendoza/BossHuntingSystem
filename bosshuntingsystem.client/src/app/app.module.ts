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
import { CacheInterceptor } from './cache-interceptor';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { JaeComponent } from './jae/jae.component';
import { DateUtilsService } from './utils/date-utils.service';
import { LoginComponent } from './login/login.component';

@NgModule({
  declarations: [
    AppComponent,
    HistoryComponent,
    NotificationsComponent,
    MembersComponent,
    DashboardComponent,
    JaeComponent,
    LoginComponent
  ],
  imports: [
    BrowserModule, CommonModule, HttpClientModule, FormsModule, ReactiveFormsModule, RouterModule,
    AppRoutingModule, NgbModule
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
