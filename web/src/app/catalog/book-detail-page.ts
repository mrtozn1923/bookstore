import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { findBook } from './books';
import { CartStore } from '../shared/cart-store';
import { formatPrice } from '../shared/format';

@Component({
  selector: 'app-book-detail-page',
  imports: [RouterLink],
  template: `
    @if (book(); as book) {
      <h2>{{ book.title }}</h2>
      <p>{{ book.author }} · ISBN {{ book.isbn }} · {{ price() }}</p>
      <button type="button" (click)="cart.add(book)">Add to cart</button>
    } @else {
      <p>Book not found.</p>
    }
    <a routerLink="/">Back to catalog</a>
  `,
})
export default class BookDetailPage {
  readonly isbn = input.required<string>();
  protected readonly cart = inject(CartStore);
  protected readonly book = computed(() => findBook(this.isbn()));
  protected readonly price = computed(() => formatPrice(this.book()?.price ?? 0));
}
