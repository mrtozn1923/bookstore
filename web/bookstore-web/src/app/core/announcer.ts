import { LiveAnnouncer } from '@angular/cdk/a11y';
import { Injectable, inject } from '@angular/core';

// TypeScript 6.0's lib.dom does not declare ariaNotify() yet (Baseline since September 2026)
declare global {
  interface Document {
    ariaNotify?(announcement: string, options?: { priority?: 'normal' | 'high' }): void;
  }
}

// Tells screen readers about a change without moving focus (WCAG 4.1.3).
@Injectable({ providedIn: 'root' })
export class Announcer {
  // The CDK adds its aria-live element to <body> as soon as it is created, so it is there before the first message
  private readonly liveAnnouncer = inject(LiveAnnouncer);

  announce(message: string, priority: 'normal' | 'high' = 'normal'): void {
    if (document.ariaNotify) {
      document.ariaNotify(message, { priority });
      return;
    }
    // Browsers without ariaNotify(): the same message through the CDK's live region
    void this.liveAnnouncer.announce(message, priority === 'high' ? 'assertive' : 'polite');
  }
}
