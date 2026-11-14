import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { csrfHeader, sessionExpired } from './core/http';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // Relative URLs only: the app and the API share one origin (the BFF), so no CORS and no API base URL
    provideHttpClient(withFetch(), withInterceptors([csrfHeader, sessionExpired]))
  ]
};
