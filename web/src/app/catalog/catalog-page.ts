import { Component, inject } from '@angular/core';
import { CartStore } from '../cart/cart-store';
import { BookCard } from './book-card';
import { CatalogService } from './catalog-service';

@Component({
  selector: 'app-catalog-page',
  imports: [BookCard],
  template: `
    <h1>Catalog</h1>
    <input
      type="search"
      aria-label="Search books"
      [value]="catalog.search()"
      (input)="catalog.search.set($any($event.target).value)"
    />
    @switch (catalog.books.status()) {
      @case ('error') {
        <p role="alert">Could not load the catalog.</p>
        <button type="button" (click)="catalog.books.reload()">Try again</button>
      }
      @case ('loading') {
        <p>Loading books…</p>
      }
      @default {
        <ul aria-label="Books">
          @for (book of catalog.inStock(); track book.isbn) {
            <li><app-book-card [book]="book" (addToCart)="cart.add($event)" /></li>
          } @empty {
            <li>No books found.</li>
          }
        </ul>
      }
    }
  `,
})
export class CatalogPage {
  protected readonly catalog = inject(CatalogService);
  protected readonly cart = inject(CartStore);
}
