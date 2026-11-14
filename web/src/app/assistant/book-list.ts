import { CurrencyPipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Cart } from './cart';
import { BookCardData } from './ui-blocks';

@Component({
  selector: 'app-book-list',
  imports: [CurrencyPipe, RouterLink],
  template: `
    <h3>{{ heading() }}</h3>
    <ul>
      @for (book of books(); track book.isbn) {
        <li>
          <a [routerLink]="['/books', book.isbn]">{{ book.title }}</a>
          <span>{{ book.author }}</span>
          <strong>{{ book.priceTry | currency: 'TRY' : 'symbol-narrow' }}</strong>
          <button type="button" (click)="cart.add(book)">Add to cart</button>
        </li>
      }
    </ul>
  `,
  styleUrl: './book-list.css',
})
export class BookList {
  readonly heading = input.required<string>();
  readonly books = input.required<BookCardData[]>();
  protected readonly cart = inject(Cart);
}
