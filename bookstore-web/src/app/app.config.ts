import { provideHttpClient } from '@angular/common/http';
import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withAutoCleanupInjectors, withComponentInputBinding } from '@angular/router';
import { provideStore } from '@ngrx/store';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { logActions } from './admin/log-actions';
import { adminOrdersPageActions } from './admin/orders.actions';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding(), withAutoCleanupInjectors()),
    provideHttpClient(),
    // Classic NgRx Store next to the Signal Stores; feature state is registered by the admin route
    provideStore({}, { metaReducers: isDevMode() ? [logActions] : [] }),
    provideStoreDevtools({
      maxAge: 25,
      logOnly: !isDevMode(),
      // NgRx 22: these actions can be dispatched from the Redux DevTools extension
      actionCreators: adminOrdersPageActions,
    }),
  ],
};
