import { provideHttpClient, withFetch } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { TitleStrategy, provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { ANALYTICS_CONFIG } from './analytics/analytics.config';
import { AnalyticsTitleStrategy } from './analytics/analytics-title.strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch()),
    { provide: TitleStrategy, useClass: AnalyticsTitleStrategy },
    { provide: ANALYTICS_CONFIG, useValue: { measurementId: 'G-TEST123' } },
  ],
};
