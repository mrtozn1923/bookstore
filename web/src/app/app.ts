import { Component, inject } from '@angular/core';
import { AnalyticsLoader } from './consent/analytics-loader';
import { ConsentBanner } from './consent/consent-banner';

@Component({
  selector: 'app-root',
  imports: [ConsentBanner],
  template: `
    <header><h1>Bookstore</h1></header>
    <main>
      <ul>
        <li>Dune</li>
        <li>Emma</li>
        <li>The Hobbit</li>
      </ul>
    </main>
    <app-consent-banner />
  `,
})
export class App {
  // Created once at start-up; it watches the consent signal and loads analytics only after a yes.
  private readonly analytics = inject(AnalyticsLoader);
}
