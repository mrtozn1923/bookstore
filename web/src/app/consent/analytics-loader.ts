import { DOCUMENT, Injectable, effect, inject } from '@angular/core';
import { ConsentService } from './consent.service';

// Basic consent mode: the analytics script is not requested at all until analytics consent exists.
@Injectable({ providedIn: 'root' })
export class AnalyticsLoader {
  private readonly document = inject(DOCUMENT);
  private loaded = false;

  constructor() {
    const consent = inject(ConsentService);
    effect(() => {
      if (consent.choices().analytics && !this.loaded) {
        this.loaded = true;
        const script = this.document.createElement('script');
        // A real site loads its tag from configuration here; this sample loads a local stub.
        script.src = '/analytics-stub.js';
        script.async = true;
        this.document.head.appendChild(script);
      }
    });
  }
}
