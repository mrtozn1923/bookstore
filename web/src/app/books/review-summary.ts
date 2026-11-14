import { httpResource } from '@angular/common/http';
import { Component, computed, input, resource, signal } from '@angular/core';
import { Availability, onDeviceAvailability, summarizeOnDevice } from './on-device-summarizer';

interface Review {
  author: string;
  rating: number;
  text: string;
}

interface Summary {
  summary: string;
  source: 'device' | 'server';
}

// Yorum özeti: cihazda üretilebiliyorsa orada, üretilemiyorsa sunucuda (progressive enhancement)
@Component({
  selector: 'app-review-summary',
  template: `
    <section aria-label="Review summary">
      @if (summary.hasValue()) {
        <p>{{ summary.value().summary }}</p>
        <p class="muted">
          {{ summary.value().source === 'device' ? 'Summarized on your device.' : 'Summarized by the Bookstore server.' }}
          AI-generated, may contain mistakes.
        </p>
      } @else if (summary.error()) {
        <p class="muted">No summary right now.</p>
      } @else {
        <p class="muted">Summarizing {{ reviews.value()?.length ?? '' }} reviews…</p>
      }
      @if (availability.value() === 'downloadable' && summary.value()?.source === 'server') {
        <button type="button" (click)="useDevice()">Summarize on this device (one-time model download)</button>
      }
      @if (progress() !== null) {
        <progress [value]="progress()" max="1" aria-label="Model download"></progress>
      }
    </section>
  `,
  styles: `.muted { color: #4b5563; font-size: 0.875rem; }`,
})
export class ReviewSummary {
  readonly isbn = input.required<string>();
  protected readonly reviews = httpResource<Review[]>(() => `/reviews/${this.isbn()}`);
  protected readonly availability = resource({ loader: () => onDeviceAvailability() });
  protected readonly progress = signal<number | null>(null);
  private readonly preferDevice = signal(false);

  private readonly reviewText = computed(() =>
    this.reviews.hasValue() ? this.reviews.value().map((r) => `(${r.rating}/5) ${r.text}`).join('\n') : undefined,
  );

  protected readonly summary = resource({
    params: () => {
      const text = this.reviewText();
      const availability = this.availability.value();
      return text === undefined || availability === undefined
        ? undefined
        : { isbn: this.isbn(), text, onDevice: availability === 'available' || this.preferDevice() };
    },
    loader: async ({ params, abortSignal }): Promise<Summary> => {
      if (params.onDevice) {
        const text = await summarizeOnDevice(params.text, (p) => this.progress.set(p), abortSignal);
        this.progress.set(null);
        return { summary: text, source: 'device' };
      }
      const response = await fetch(`/reviews/${params.isbn}/summary`, { signal: abortSignal });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }
      return response.json();
    },
  });

  // Model indirmesi kullanıcının tıklamasıyla başlar (user activation); sayfa kendiliğinden gigabaytlarca indirmez
  protected useDevice(): void {
    this.preferDevice.set(true);
  }
}
