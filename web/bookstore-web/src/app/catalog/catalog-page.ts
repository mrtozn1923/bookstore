import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BookSummary, PagedResponse } from '../core/models';

@Component({
  selector: 'app-catalog-page',
  imports: [RouterLink, CurrencyPipe],
  template: `
    <form (submit)="$event.preventDefault(); search.set(query.value)">
      <input #query type="search" placeholder="Search by title or author" [value]="search()" />
      <button type="submit">Search</button>
    </form>

    @if (books.isLoading()) {
      <p>Loading…</p>
    } @else if (books.error()) {
      <p class="error">The catalog is not available right now. Try again in a moment.</p>
    } @else if (books.hasValue()) {
      <ul class="books">
        @for (book of books.value().items; track book.isbn) {
          <li>
            <a [routerLink]="['/books', book.isbn]">{{ book.title }}</a>
            <span>{{ book.author }}</span>
            <span>{{ book.price | currency: book.currency }}</span>
          </li>
        } @empty {
          <li>No books found.</li>
        }
      </ul>
    }
  `
})
export class CatalogPage {
  protected readonly search = signal('');
  // The request is a function of the signal: a new search cancels the previous request and starts a new one
  protected readonly books = httpResource<PagedResponse<BookSummary>>(() => ({
    url: '/api/catalog/books',
    params: { search: this.search(), pageSize: 20 }
  }));
}
