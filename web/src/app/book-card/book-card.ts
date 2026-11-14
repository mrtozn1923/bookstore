import { Component, computed, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { MatButton } from '@angular/material/button';
import { Book } from '../book';

@Component({
  selector: 'app-book-card',
  imports: [CurrencyPipe, MatButton],
  templateUrl: './book-card.html',
  styleUrl: './book-card.scss',
})
export class BookCard {
  readonly book = input.required<Book>();
  protected readonly stockState = computed(() => {
    const stock = this.book().stock;
    return stock === 0 ? 'out' : stock < 5 ? 'low' : 'in-stock';
  });
}
