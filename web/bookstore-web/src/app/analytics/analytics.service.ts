import { isPlatformBrowser } from '@angular/common';
import { DOCUMENT, Injectable, PLATFORM_ID, inject } from '@angular/core';
import { ANALYTICS_CONFIG } from './analytics.config';
import { loadGoogleTag } from './google-tag';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly doc = inject(DOCUMENT);
  private readonly config = inject(ANALYTICS_CONFIG);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private gtag?: (...args: unknown[]) => void;
  private previousUrl = '';

  start(): void {
    // no tag during SSR or when no measurement ID is configured
    if (!this.isBrowser || !this.config.measurementId || this.gtag) {
      return;
    }

    this.gtag = loadGoogleTag(this.doc, this.config.measurementId);
    this.rememberClientId();
    this.previousUrl = this.doc.referrer;
  }

  // Called by AnalyticsTitleStrategy after the router has set document.title.
  pageView(): void {
    const location = this.doc.location.href;
    this.gtag?.('event', 'page_view', {
      page_location: location,
      page_referrer: this.previousUrl,
      page_title: this.doc.title,
    });
    this.previousUrl = location;
  }

  codeCopied(language: string): void {
    this.gtag?.('event', 'code_copy', { language });
  }

  seriesNavigated(series: string, direction: 'next' | 'previous'): void {
    this.gtag?.('event', 'series_navigation', { series, direction });
  }

  // The server reads this first-party cookie for Measurement Protocol events.
  private rememberClientId(): void {
    this.gtag?.('get', this.config.measurementId, 'client_id', (clientId: string) => {
      this.doc.cookie = `bs_ga_cid=${clientId}; Path=/; Max-Age=${60 * 60 * 24 * 90}; SameSite=Lax; Secure`;
    });
  }
}
