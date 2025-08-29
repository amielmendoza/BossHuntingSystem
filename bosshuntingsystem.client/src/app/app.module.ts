import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HistoryComponent } from './history/history.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { MembersComponent } from './members/members.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { CacheInterceptor } from './cache-interceptor';
import { DateUtilsService } from './utils/date-utils.service';

@NgModule({
  declarations: [
    AppComponent,
    HistoryComponent,
    NotificationsComponent,
    MembersComponent,
    DashboardComponent
  ],
  imports: [
    BrowserModule, CommonModule, HttpClientModule, FormsModule, RouterModule,
    AppRoutingModule, NgbModule
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: CacheInterceptor,
      multi: true
    },
    DateUtilsService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
