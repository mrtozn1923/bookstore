import { httpResource } from '@angular/common/http';
import { Component, computed, effect, inject, input, linkedSignal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { extendResource, withPreviousValueOnLoading } from '@ngrx/signals/resource';
import { apiUrl } from '../api';
import { CartStore } from '../cart/cart-store';
import type { Book, BookPage } from '../models';
import { Categories } from './categories';
import { RecentlyViewedStore } from './recently-viewed-store';
import { toPage, toSearch, toSort, type SortOrder } from './catalog-params';

@Component({
  selector: 'app-catalog-page',
  imports: [RouterLink],
  template: `
    <h1>Catalog</h1>
    <nav class="categories">
      <a routerLink="/" [queryParams]="{ category: null, page: null }" queryParamsHandling="merge">All</a>
      @for (c of categories.list.value(); track c.slug) {
        <a routerLink="/" [queryParams]="{ category: c.slug, page: null }" queryParamsHandling="merge">
          {{ c.name }}
        </a>
      }
    </nav>
    <p>
      <input class="search" placeholder="Search titles" [value]="q()" (input)="search($any($event.target).value)" />
      <select class="sort" [value]="sort()" (change)="sortBy($any($event.target).value)">
        <option value="title">Title</option>
        <option value="price">Price</option>
      </select>
    </p>
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
        <button class="prev" [disabled]="page() === 1" (click)="goTo(page() - 1)">Previous</button>
        <button class="next" [disabled]="!hasNextPage()" (click)="goTo(page() + 1)">Next</button>
      </p>
    } @else if (books.error()) {
      <p>Could not load the books.</p>
    } @else {
      <p>Loading...</p>
    }
    @if (recent.viewedEntities().length) {
      <p class="recent">
        Recently viewed:
        @for (book of recent.viewedEntities(); track book.isbn) {
          <a [routerLink]="['/books', book.isbn]">{{ book.title }}</a>
        }
      </p>
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
  readonly #router = inject(Router);

  // URL state: ?category=classics&q=emm&sort=price&page=2, bound by withComponentInputBinding()
  // and validated by the transform functions (the URL can contain anything)
  readonly category = input<string>();
  readonly q = input('', { transform: toSearch });
  readonly sort = input<SortOrder, string | undefined>('title', { transform: toSort });
  readonly page = input<number, string | undefined>(1, { transform: toPage });

  // Server state: derived from the URL state, fetched again when any of it changes;
  // the previous page stays on screen while the next one loads (NgRx resource extension)
  protected readonly books = extendResource(
    httpResource<BookPage>(() => {
      const params: Record<string, string | number> = { page: this.page(), sort: this.sort() };
      const category = this.category();
      if (category) params['category'] = category;
      if (this.q()) params['q'] = this.q();
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
    return `${total} books, page ${page} of ${Math.max(1, Math.ceil(total / pageSize))}`;
  });

  protected readonly hasNextPage = computed(() => {
    if (this.books.status() === 'error' || !this.books.hasValue()) return false;
    const { total, page, pageSize } = this.books.value();
    return page * pageSize < total;
  });

  protected readonly categories = inject(Categories);
  protected readonly recent = inject(RecentlyViewedStore);

  constructor() {
    effect(() => {
      const items = this.books.hasValue() ? this.books.value().items.map((b) => b.title) : '-';
      console.log(`[books] ${this.books.status()}: ${items}`);
    });
  }

  // Each change writes the URL; the inputs, and through them the resource, follow the URL
  protected goTo(page: number): void {
    this.#updateQuery({ page: page === 1 ? null : page });
  }

  protected sortBy(sort: string): void {
    this.#updateQuery({ sort: sort === 'title' ? null : sort, page: null });
  }

  protected search(q: string): void {
    // typing is one step for the user: replace the history entry instead of adding one per key
    this.#updateQuery({ q: q || null, page: null }, { replaceUrl: true });
  }

  #updateQuery(queryParams: Record<string, string | number | null>, extras = {}): void {
    void this.#router.navigate([], { queryParams, queryParamsHandling: 'merge', ...extras });
  }
}
