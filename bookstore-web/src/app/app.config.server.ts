import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { API_ORIGIN } from './core/api-origin';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    // The prerender step runs at build time and needs a running Bookstore API
    { provide: API_ORIGIN, useValue: process.env['BOOKSTORE_API'] ?? 'http://127.0.0.1:5300' },
  ],
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
