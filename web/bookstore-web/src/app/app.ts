import { Component, effect, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AnalyticsService } from './analytics/analytics.service';
import { ConsentBanner } from './consent/consent-banner';
import { ConsentService } from './consent/consent.service';
import { RumService } from './rum/rum.service';

@Component({
  imports: [RouterOutlet, ConsentBanner],
  selector: 'app-root',
  template: `
    <router-outlet />
    <app-consent-banner />
  `,
})
export class App {
  constructor() {
    const analytics = inject(AnalyticsService);
    const consent = inject(ConsentService);
    inject(RumService).start();
    effect(() => {
      if (consent.analytics() === 'granted') {
        analytics.start();
      } else {
        analytics.stop();
      }
    });
  }
}
