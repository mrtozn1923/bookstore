import { LOCALE_ID, enableProdMode, provideZoneChangeDetection } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeTr from '@angular/common/locales/tr';
import { bootstrapApplication } from '@angular/platform-browser';
import { PreloadAllModules, TitleStrategy, provideRouter, withComponentInputBinding, withPreloading } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

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

registerLocaleData(localeTr);

bootstrapApplication(AppComponent, {
  providers: [
    provideZoneChangeDetection(),
    provideRouter(routes, withPreloading(PreloadAllModules), withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    { provide: LOCALE_ID, useValue: 'tr' },
    {
      provide: LoggerService,
      useFactory: () => environment.production ? new SilentLoggerService() : new LoggerService()
    },
    { provide: APP_CONFIG, useValue: { appName: 'Bookstore', pageSize: 20 } },
    { provide: TitleStrategy, useClass: BookstoreTitleStrategy }
  ]
}).catch(err => console.error(err));
