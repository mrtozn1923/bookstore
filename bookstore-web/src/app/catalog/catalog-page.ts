import { httpResource } from '@angular/common/http';
import { Component, computed, effect, inject, input, linkedSignal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { extendResource, withPreviousValueOnLoading } from '@ngrx/signals/resource';
import { apiUrl } from '../api';
import { CartStore } from '../cart/cart-store';
import type { Book, BookPage } from '../models';
import { Categories } from './categories';

@Component({
  selector: 'app-catalog-page',
  imports: [RouterLink],
  template: `
    <h1>Catalog</h1>
    <nav class="categories">
      <a routerLink="/">All</a>
      @for (c of categories.list.value(); track c.slug) {
        <a routerLink="/" [queryParams]="{ category: c.slug }">{{ c.name }}</a>
      }
    </nav>
    @if (books.hasValue()) {
      <p class="summary">{{ summary() }}</p>
      @if (books.isLoading()) {
        <p class="updating">Updating...</p>
      }
      <ul class="books">
        @for (book of books.value().items; track book.isbn) {
          <li>
            <button class="cover" (click)="coverOf.set(book)">{{ book.title }}</button>
            {{ book.author }} - {{ book.price }} TRY - {{ book.stock }} in stock
            <a class="details" [routerLink]="['/books', book.isbn]">Details</a>
            <button class="add" (click)="cart.add(book)">Add to cart</button>
          </li>
        }
      </ul>
      <p class="pager">
        <button class="prev" [disabled]="page() === 1" (click)="page.set(page() - 1)">Previous</button>
        <button class="next" [disabled]="!hasNextPage()" (click)="page.set(page() + 1)">Next</button>
      </p>
    } @else if (books.error()) {
      <p>Could not load the books.</p>
    } @else {
      <p>Loading...</p>
    }
    @if (coverOf(); as book) {
      <div class="cover-dialog" role="dialog">
        <p>{{ book.title }} (large cover)</p>
        <button (click)="coverOf.set(null)">Close</button>
      </div>
    }
  `,
})
export class CatalogPage {
  // URL state: ?category=classics, bound by withComponentInputBinding()
  readonly category = input<string>();

  // Local state that starts again from 1 whenever the category changes
  protected readonly page = linkedSignal({ source: this.category, computation: () => 1 });

  // Server state: a copy of the API's data, fetched again when the category or page changes;
  // the previous page stays on screen while the next one loads (NgRx resource extension)
  protected readonly books = extendResource(
    httpResource<BookPage>(() => {
      const params: Record<string, string | number> = { page: this.page() };
      const category = this.category();
      if (category) params['category'] = category;
      return { url: `${apiUrl}/books`, params };
    }),
    withPreviousValueOnLoading(),
  );

  // Global client state: shared with the header and the cart page
  protected readonly cart = inject(CartStore);

  // Local UI state: the cover dialog closes when the category changes
  protected readonly coverOf = linkedSignal<string | undefined, Book | null>({
    source: this.category,
    computation: () => null,
  });

  // Derived state: computed from other state, never stored
  protected readonly summary = computed(() => {
    // status() is read first on purpose: while loading, the extended value() is not tracked
    if (this.books.status() === 'error' || !this.books.hasValue()) return '';
    const { total, page, pageSize } = this.books.value();
    return `${total} books, page ${page} of ${Math.ceil(total / pageSize)}`;
  });

  protected readonly hasNextPage = computed(() => {
    if (this.books.status() === 'error' || !this.books.hasValue()) return false;
    const { total, page, pageSize } = this.books.value();
    return page * pageSize < total;
  });

  protected readonly categories = inject(Categories);

  constructor() {
    effect(() => {
      const items = this.books.hasValue() ? this.books.value().items.map((b) => b.title) : '-';
      console.log(`[books] ${this.books.status()}: ${items}`);
    });
  }
}
