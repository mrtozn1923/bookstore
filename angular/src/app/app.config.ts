import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding, withViewTransitions } from '@angular/router';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // every router navigation runs inside document.startViewTransition(); without browser support it navigates as usual
    provideRouter(routes, withComponentInputBinding(), withViewTransitions()),
  ],
};
