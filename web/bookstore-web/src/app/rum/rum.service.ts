import { isPlatformBrowser } from '@angular/common';
import { DOCUMENT, Injectable, PLATFORM_ID, inject } from '@angular/core';
import { onCLS, onINP, onLCP, type Metric } from 'web-vitals';

interface RumBeacon {
  id: string;
  name: string;
  value: number;
  pageGroup: string;
}

// Field data from our own visitors: no cookie, no user ID, only the metric and the route pattern.
@Injectable({ providedIn: 'root' })
export class RumService {
  private readonly doc = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly queue = new Map<string, RumBeacon>();
  private pageGroup = 'other';
  private started = false;

  start(): void {
    if (!this.isBrowser || this.started) {
      return;
    }
    this.started = true;

    // web-vitals may report the same metric again with the same id: keep the latest value
    const report = (m: Metric) =>
      this.queue.set(m.id, { id: m.id, name: m.name, value: m.value, pageGroup: this.pageGroup });
    onLCP(report);
    onINP(report);
    onCLS(report);

    this.doc.addEventListener('visibilitychange', () => {
      if (this.doc.visibilityState === 'hidden') {
        this.flush();
      }
    });
  }

  // Like CrUX, metrics belong to the page that was loaded, not to later SPA navigations.
  landedOn(routePattern: string): void {
    if (this.pageGroup === 'other') {
      this.pageGroup = routePattern;
    }
  }

  private flush(): void {
    if (this.queue.size === 0) {
      return;
    }
    const body = new Blob([JSON.stringify([...this.queue.values()])], { type: 'application/json' });
    navigator.sendBeacon('/rum', body);
    this.queue.clear();
  }
}
