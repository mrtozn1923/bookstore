import { InjectionToken } from '@angular/core';

export interface AnalyticsConfig {
  // G-... ID of the GA4 web data stream; empty string turns analytics off
  measurementId: string;
}

export const ANALYTICS_CONFIG = new InjectionToken<AnalyticsConfig>('ANALYTICS_CONFIG', {
  factory: () => ({ measurementId: '' }),
});
