import { Component, input } from '@angular/core';
import { httpResource } from '@angular/common/http';

// Same shape as BookDetailsView in Bookstore.Gateway; null means "unknown".
interface BookDetailsView {
  isbn: string;
  title: string;
  author: string;
  price: number;
  available: number | null;
  purchasedBefore: boolean | null;
  warnings: string[];
}

@Component({
  selector: 'app-book-details',
  template: `
    @if (book.value(); as b) {
      <h1>{{ b.title }}</h1>
      <p>{{ b.author }} · {{ b.price }} TRY</p>
      @if (b.available === null) {
        <p>Stock information is unavailable right now.</p>
      } @else {
        <p>{{ b.available }} in stock</p>
      }
      @if (b.purchasedBefore) {
        <p>You bought this book before.</p>
      }
    } @else if (book.error()) {
      <p>Book not found.</p>
    }
  `,
})
export class BookDetails {
  readonly isbn = input.required<string>();

  // Relative URL: the gateway serves both this app and /api, so there is no CORS.
  protected readonly book = httpResource<BookDetailsView>(() => `/api/bff/books/${this.isbn()}`);
}
