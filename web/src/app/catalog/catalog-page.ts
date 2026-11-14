import { Component, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Book } from './book';
import { books } from './books';
import { groupBy } from '../shared/collections';
import { fromNow } from '../shared/dates';
import { formatPrice } from '../shared/format';

@Component({
  selector: 'app-catalog-page',
  imports: [RouterLink],
  template: `
    <input type="search" placeholder="Search by title or author" aria-label="Search books"
           [value]="query()" (input)="query.set($any($event.target).value)">
    @for (group of groups(); track group.author) {
      <h2>{{ group.author }}</h2>
      <ul>
        @for (book of group.books; track book.isbn) {
          <li>
            <a [routerLink]="['/books', book.isbn]">{{ book.title }}</a>
            {{ price(book) }} · added {{ added(book) }}
          </li>
        }
      </ul>
    } @empty {
      <p>No books match "{{ query() }}".</p>
    }
  `,
})
export class CatalogPage {
  protected readonly query = signal('');

  protected readonly groups = computed(() => {
    const term = this.query().trim().toLowerCase();
    const matches = books.filter(
      (book) => book.title.toLowerCase().includes(term) || book.author.toLowerCase().includes(term),
    );
    const byAuthor = groupBy(
      [...matches].sort((a, b) => a.title.localeCompare(b.title)),
      (book) => book.author,
    );
    return [...byAuthor.keys()]
      .sort((a, b) => a.localeCompare(b))
      .map((author) => ({ author, books: byAuthor.get(author) ?? [] }));
  });

  protected price(book: Book): string {
    return formatPrice(book.price);
  }

  protected added(book: Book): string {
    return fromNow(book.addedAt);
  }
}
