import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component } from '@angular/core';
import { Book } from './book';

@Component({
  selector: 'app-catalog-page',
  imports: [CurrencyPipe],
  template: `
    <h1>Catalog</h1>
    @if (books.hasValue()) {
      <ul>
        @for (book of books.value(); track book.isbn) {
          <li>{{ book.title }}: {{ book.price | currency: 'TRY' }}</li>
        }
      </ul>
    } @else if (books.error()) {
      <p>The catalog could not be loaded.</p>
    }
  `,
})
export class CatalogPage {
  protected readonly books = httpResource<Book[]>(() => '/api/books');
}
