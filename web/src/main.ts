/// <reference types="@angular/localize" />

import { enableProdMode, provideZoneChangeDetection } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { PreloadAllModules, TitleStrategy, provideRouter, withComponentInputBinding, withPreloading } from '@angular/router';
import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';

import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { APP_CONFIG } from './app/app-config';
import { authInterceptor } from './app/auth.interceptor';
import { errorInterceptor } from './app/error.interceptor';
import { BookstoreTitleStrategy } from './app/bookstore-title.strategy';
import { LoggerService, SilentLoggerService } from './app/logger.service';
import { environment } from './environments/environment';

if (environment.production) {
  enableProdMode();
}

bootstrapApplication(AppComponent, {
  providers: [
    provideZoneChangeDetection(),
    provideRouter(routes, withPreloading(PreloadAllModules), withComponentInputBinding()),
    provideHttpClient(withXhr(), withInterceptors([authInterceptor, errorInterceptor])),
    {
      provide: LoggerService,
      useFactory: () => environment.production ? new SilentLoggerService() : new LoggerService()
    },
    { provide: APP_CONFIG, useValue: { appName: 'Bookstore', pageSize: 20 } },
    { provide: TitleStrategy, useClass: BookstoreTitleStrategy }
  ]
}).catch(err => console.error(err));
