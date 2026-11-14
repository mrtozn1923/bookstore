import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Book } from './book';

@Component({
  selector: 'app-catalog-page',
  imports: [CurrencyPipe, RouterLink],
  template: `
    <h1>Catalog</h1>
    @if (books.hasValue()) {
      <ul>
        @for (book of books.value(); track book.isbn) {
          <li>
            <a [routerLink]="['/books', book.isbn]">{{ book.title }}</a>:
            {{ book.price | currency: 'TRY' }}
          </li>
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
