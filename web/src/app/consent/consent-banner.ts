import { Component, inject, signal } from '@angular/core';
import { ConsentService } from './consent.service';

@Component({
  selector: 'app-consent-banner',
  template: `
    @if (!consent.decided() || editing()) {
      <section class="banner" role="dialog" aria-label="Cookie consent">
        <p>
          We use necessary cookies to run the store. With your consent we also use analytics and marketing cookies.
          <a href="/privacy/cookies">Cookie notice</a>
        </p>
        @if (editing()) {
          <label><input type="checkbox" checked disabled> Necessary (always on)</label>
          <label><input type="checkbox" name="analytics" [checked]="analytics()" (change)="analytics.set(isChecked($event))"> Analytics</label>
          <label><input type="checkbox" name="marketing" [checked]="marketing()" (change)="marketing.set(isChecked($event))"> Marketing</label>
          <button class="choice" (click)="save(analytics(), marketing())">Save choices</button>
        } @else {
          <button class="choice" (click)="save(true, true)">Accept all</button>
          <button class="choice" (click)="save(false, false)">Reject all</button>
          <button class="choice" (click)="openPreferences()">Preferences</button>
        }
      </section>
    } @else {
      <button class="settings" (click)="openPreferences()">Cookie settings</button>
    }
  `,
  styles: `
    .banner { position: fixed; inset: auto 0 0 0; padding: 1rem; background: #1e293b; color: #f8fafc; }
    .choice { min-width: 9rem; padding: 0.6rem 1rem; margin-right: 0.5rem; font-size: 1rem;
              background: #f8fafc; color: #0f172a; border: 0; border-radius: 6px; }
    label { display: block; margin: 0.25rem 0; }
    .settings { position: fixed; left: 0.5rem; bottom: 0.5rem; font-size: 0.8rem; }
  `,
})
export class ConsentBanner {
  protected readonly consent = inject(ConsentService);
  protected readonly editing = signal(false);
  // Unticked unless the visitor already gave consent: no pre-selected boxes.
  protected readonly analytics = signal(false);
  protected readonly marketing = signal(false);

  protected openPreferences(): void {
    this.analytics.set(this.consent.choices().analytics);
    this.marketing.set(this.consent.choices().marketing);
    this.editing.set(true);
  }

  protected async save(analytics: boolean, marketing: boolean): Promise<void> {
    this.editing.set(false);
    await this.consent.save({ analytics, marketing });
  }

  protected isChecked(event: Event): boolean {
    return (event.target as HTMLInputElement).checked;
  }
}
