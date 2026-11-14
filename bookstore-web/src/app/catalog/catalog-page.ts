import { AsyncPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { apiUrl } from '../api';
import { CartStore } from '../cart/cart-store';
import type { Book } from './book';
import { CategoryService } from './category-service';

@Component({
  selector: 'app-catalog-page',
  imports: [AsyncPipe],
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
    <ul>
      @for (book of books(); track book.isbn) {
        <li>
          {{ book.title }} - {{ book.price }} TRY
          <button type="button" (click)="cart.add(book)" [disabled]="book.stock === 0">
            Add to cart
          </button>
        </li>
      }
    </ul>
  `,
})
export class CatalogPage {
  protected readonly cart = inject(CartStore);
  protected readonly books = toSignal(inject(HttpClient).get<Book[]>(`${apiUrl}/books`), {
    initialValue: [],
  });
  protected readonly categories$ = inject(CategoryService).categories$;
}
