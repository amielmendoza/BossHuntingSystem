import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HistoryComponent } from './history/history.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { CacheInterceptor } from './cache-interceptor';

@NgModule({
  declarations: [
    AppComponent,
    HistoryComponent,
    NotificationsComponent
  ],
  imports: [
    BrowserModule, CommonModule, HttpClientModule, FormsModule,
    AppRoutingModule,
    NgbModule
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: CacheInterceptor,
      multi: true
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
