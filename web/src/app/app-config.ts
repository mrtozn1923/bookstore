import { InjectionToken } from '@angular/core';

export interface AppConfig {
  appName: string;
  pageSize: number;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('app.config');
