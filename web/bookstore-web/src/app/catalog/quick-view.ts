import type { OverlayRef } from '@angular/cdk/overlay';
import { DestroyRef, inject, Injectable, Injector, signal } from '@angular/core';

// Provided by the page that uses it: one panel at a time, gone together with the page
@Injectable()
export class QuickView {
  private readonly injector = inject(Injector);
  private overlayRef: OverlayRef | null = null;
  private trigger: HTMLElement | null = null;
  private destroyed = false;
  readonly openIsbn = signal<string | null>(null);

  constructor() {
    // Without this the panel and its backdrop stay on screen after the page is gone
    inject(DestroyRef).onDestroy(() => {
      this.destroyed = true;
      this.overlayRef?.dispose();
    });
  }

  async open(isbn: string, card: HTMLElement, trigger: HTMLElement): Promise<void> {
    this.close();
    const { attachQuickView } = await import('./quick-view-overlay');
    // The page may be gone while the chunk was loading
    if (this.destroyed) {
      return;
    }
    this.trigger = trigger;
    this.overlayRef = attachQuickView(this.injector, isbn, card, () => this.close());
    this.openIsbn.set(isbn);
  }

  close(): void {
    if (!this.overlayRef) {
      return;
    }
    this.overlayRef.dispose();
    this.overlayRef = null;
    this.openIsbn.set(null);
    // Focus goes back to the button that opened the panel
    this.trigger?.focus();
    this.trigger = null;
  }
}
