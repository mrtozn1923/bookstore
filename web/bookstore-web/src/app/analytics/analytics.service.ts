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
  private navigated = false;

  start(): void {
    // no tag during SSR or when no measurement ID is configured
    if (!this.isBrowser || !this.config.measurementId || this.gtag) {
      return;
    }

    this.gtag = loadGoogleTag(this.doc, this.config.measurementId);
    this.rememberClientId();
    this.previousUrl = this.doc.referrer;
    // consent given on an already open page: count that page once;
    // otherwise AnalyticsTitleStrategy sends the first page_view
    if (this.navigated) {
      this.pageView();
    }
  }

  // Consent withdrawn: tell the tag, then stop sending anything from this page.
  stop(): void {
    this.gtag?.('consent', 'update', { analytics_storage: 'denied' });
    this.gtag = undefined;
  }

  // Called by AnalyticsTitleStrategy after the router has set document.title.
  // (Injecting Router here would be a circular dependency: Router -> TitleStrategy -> this.)
  pageView(): void {
    this.navigated = true;
    if (!this.gtag) {
      return;
    }
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
