import { AsyncPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { apiUrl } from '../api';
import { CartStore } from '../cart/cart-store';
import { loadingState } from '../core/operators';
import { errorMessage } from '../core/problem-details';
import type { Book } from './book';
import { CategoryService } from './category-service';

@Component({
  selector: 'app-catalog-page',
  imports: [AsyncPipe, RouterLink],
  template: `
    <h1>Catalog</h1>
    <nav class="categories">
      @for (category of categories$ | async; track category) {
        <a href="#">{{ category }}</a>
      }
    </nav>
    <label>
      Category
      <select>
        @for (category of categories$ | async; track category) {
          <option>{{ category }}</option>
        }
      </select>
    </label>
    @let state = books();
    @switch (state.status) {
      @case ('loading') {
        <p class="books-state">Loading books...</p>
      }
      @case ('error') {
        <p class="books-state">{{ errorMessage(state.error) }}</p>
      }
      @case ('success') {
        <ul>
          @for (book of state.data; track book.isbn) {
            <li>
              <a [routerLink]="['/books', book.isbn]">{{ book.title }}</a> - {{ book.price }} TRY
              <button type="button" (click)="cart.add(book)" [disabled]="book.stock === 0">
                Add to cart
              </button>
            </li>
          }
        </ul>
      }
    }
  `,
})
export class CatalogPage {
  protected readonly cart = inject(CartStore);
  // the interceptor has already retried; the page shows loading, the list or a message
  protected readonly books = toSignal(
    inject(HttpClient).get<Book[]>(`${apiUrl}/books`).pipe(loadingState()),
    { requireSync: true },
  );
  protected readonly errorMessage = errorMessage;
  protected readonly categories$ = inject(CategoryService).categories$;
}
