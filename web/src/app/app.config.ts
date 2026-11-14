import { provideHttpClient, withFetch } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideExperimentalWebMcpTools } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { BOOKSTORE_WEBMCP_TOOLS } from './webmcp-tools';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch()),
    // Deneysel: tarayıcı WebMCP desteklemiyorsa Angular hiçbir şey kaydetmez
    provideExperimentalWebMcpTools(BOOKSTORE_WEBMCP_TOOLS),
  ]
};
