import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideClientHydration } from '@angular/platform-browser';
import { IMAGE_CONFIG, IMAGE_LOADER } from '@angular/common';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { apiOriginInterceptor } from './core/api-origin';
import { COVER_WIDTHS, coverLoader } from './core/cover-loader';
import { provideRum } from './core/rum';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([apiOriginInterceptor])),
    provideClientHydration(),
    { provide: IMAGE_LOADER, useValue: coverLoader },
    { provide: IMAGE_CONFIG, useValue: { breakpoints: COVER_WIDTHS } },
    // Lab: every visit is measured; in production sample (e.g. 0.1) according to traffic
    provideRum({ endpoint: '/api/rum', sampleRate: 1 }),
  ],
};
