import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { Cart } from '../core/cart';
import { BookPage as Book } from '../core/models';

@Component({
  selector: 'app-book-page',
  imports: [CurrencyPipe],
  template: `
    @if (book.hasValue()) {
      @let b = book.value();
      <h1>{{ b.title }}</h1>
      <p>{{ b.author }} · {{ b.category }}</p>
      <p>{{ b.description }}</p>
      <p class="price">{{ b.price | currency: b.currency }}</p>
      <!-- stock is null when Inventory did not answer in time (post 09): the page still works -->
      @switch (b.stock?.status) {
        @case ('InStock') { <p>In stock</p> }
        @case ('Low') { <p>Only {{ b.stock?.available }} left</p> }
        @case ('OutOfStock') { <p class="error">Out of stock</p> }
        @default { <p class="muted">Stock information is not available right now.</p> }
      }
      <button (click)="addToCart(b)" [disabled]="b.stock?.status === 'OutOfStock'">Add to cart</button>
      @if (added()) { <p>Added to your cart.</p> }
    } @else if (book.error()) {
      <p class="error">This book cannot be shown right now.</p>
    } @else {
      <p>Loading…</p>
    }
  `
})
export class BookPage {
  // Bound from the route parameter /books/:isbn (withComponentInputBinding)
  readonly isbn = input.required<string>();
  protected readonly book = httpResource<Book>(() => `/api/books/${this.isbn()}`);
  protected readonly added = signal(false);
  private readonly cart = inject(Cart);

  protected addToCart(book: Book): void {
    this.cart.add({ isbn: book.isbn, title: book.title, unitPrice: book.price, currency: book.currency });
    this.added.set(true);
  }
}
