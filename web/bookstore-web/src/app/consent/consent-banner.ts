import { Component, inject } from '@angular/core';
import { ConsentService } from './consent.service';

// "Accept" and "Reject" look the same: no pre-selected or highlighted choice.
@Component({
  selector: 'app-consent-banner',
  template: `
    @if (consent.analytics() === 'unknown') {
      <section role="dialog" aria-label="Analytics cookies">
        <p>We use Google Analytics cookies only if you allow them.</p>
        <button type="button" class="choice" (click)="consent.grant()">Accept analytics</button>
        <button type="button" class="choice" (click)="consent.deny()">Reject</button>
      </section>
    } @else {
      <button type="button" class="settings" (click)="consent.reopen()">Cookie settings</button>
    }
  `,
})
export class ConsentBanner {
  protected readonly consent = inject(ConsentService);
}
