import { HttpClient } from '@angular/common/http';
import { DOCUMENT, Injectable, inject, signal } from '@angular/core';

export type ConsentState = 'unknown' | 'granted' | 'denied';

@Injectable({ providedIn: 'root' })
export class ConsentService {
  private readonly http = inject(HttpClient);
  private readonly doc = inject(DOCUMENT);
  private static readonly DeniedKey = 'analytics-consent';

  readonly analytics = signal<ConsentState>(this.read());

  grant(): void {
    // the server writes .AspNet.Consent; Razor pages and the PDF endpoint read it
    this.http.post('/consent/analytics/grant', null).subscribe(() => {
      this.doc.defaultView?.localStorage.removeItem(ConsentService.DeniedKey);
      this.analytics.set('granted');
    });
  }

  deny(): void {
    // remember the "no" too, so the banner is not shown on every visit
    this.doc.defaultView?.localStorage.setItem(ConsentService.DeniedKey, 'denied');
    this.http.post('/consent/analytics/withdraw', null).subscribe(() => this.analytics.set('denied'));
  }

  reopen(): void {
    this.analytics.set('unknown');
  }

  private read(): ConsentState {
    if (this.doc.cookie.split('; ').includes('.AspNet.Consent=yes')) {
      return 'granted';
    }
    return this.doc.defaultView?.localStorage?.getItem(ConsentService.DeniedKey) === 'denied' ? 'denied' : 'unknown';
  }
}
