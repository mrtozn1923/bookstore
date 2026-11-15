import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BookDetails } from '../core/api-models';

// The panel's content: a ComponentPortal renders it inside the overlay, far away from the card in the DOM
@Component({
  selector: 'app-book-quick-view',
  imports: [CurrencyPipe, RouterLink],
  host: {
    role: 'dialog',
    'aria-labelledby': 'quick-view-title',
    tabindex: '-1',
    class: 'quick-view',
  },
  template: `
    @if (book.value(); as b) {
      <h2 id="quick-view-title">{{ b.title }}</h2>
      <p class="muted">{{ b.author }} · {{ b.publishedYear }}</p>
      <p>{{ b.description }}</p>
      <p>
        <strong>{{ b.price | currency: 'TRY' : 'symbol-narrow' }}</strong>
        <span class="muted"> · {{ b.available > 0 ? b.available + ' in stock' : 'Out of stock' }}</span>
      </p>
      <div class="actions">
        <a [routerLink]="['/books', b.isbn]">View details</a>
        <button type="button" (click)="closed.emit()">Close</button>
      </div>
    } @else if (book.error()) {
      <h2 id="quick-view-title">Not available</h2>
      <button type="button" (click)="closed.emit()">Close</button>
    } @else {
      <p id="quick-view-title" class="muted">Loading…</p>
    }
  `,
  styles: `
    :host {
      display: block;
      width: 280px;
      padding: 16px;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      background: #ffffff;
      box-shadow: 0 8px 24px rgb(0 0 0 / 0.15);
    }
    h2 { margin: 0 0 4px; font-size: 1.1rem; }
    .actions { display: flex; justify-content: space-between; align-items: center; }
  `,
})
export class BookQuickView {
  readonly isbn = input.required<string>();
  readonly closed = output<void>();

  protected readonly book = httpResource<BookDetails>(() => `/api/books/${this.isbn()}`);
}
