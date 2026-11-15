import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import type { Subscription } from 'rxjs';
import { BookApi, type BookDto } from '../api/book-api.service';
import { CartStore } from '../cart/cart-store';
import { stockLabel } from './labels';

@Component({
  selector: 'app-catalog-page',
  imports: [CurrencyPipe],
  template: `
    <h1>Catalog</h1>
    <form class="search" (submit)="search(term.value); $event.preventDefault()">
      <label for="search">Search books</label>
      <input id="search" #term type="search" autocomplete="off" />
      <button type="submit">Search</button>
    </form>

    @if (books(); as list) {
      <p class="result-count">{{ list.length }} {{ list.length === 1 ? 'book' : 'books' }} found</p>
      <ul class="book-list" aria-label="Search results">
        @for (book of list; track book.id) {
          <li class="book-card">
            <h2>{{ book.title }}</h2>
            <p class="price">{{ book.price | currency: 'TRY' : 'symbol-narrow' }}</p>
            <p class="stock">{{ stockLabel(book) }}</p>
            <button type="button" class="btn-primary" [disabled]="book.stock === 0" (click)="cart.add(book)">
              Add to cart
            </button>
          </li>
        }
      </ul>
    } @else {
      <p>Loading books…</p>
    }
  `,
})
export class CatalogPage {
  private readonly api = inject(BookApi);
  protected readonly cart = inject(CartStore);
  protected readonly books = signal<BookDto[] | null>(null);
  protected readonly stockLabel = stockLabel;
  private pending?: Subscription;

  constructor() {
    this.search('');
  }

  protected search(term: string): void {
    this.books.set(null);
    // Önceki istek hâlâ sürüyorsa iptal et; geç gelen cevap yeni sonucu ezmesin
    this.pending?.unsubscribe();
    this.pending = this.api.search(term).subscribe((books) => this.books.set(books));
  }
}
