import { Component, computed, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Book } from './book';

type StockState = 'in-stock' | 'low' | 'sold-out';

// full class names per state: Tailwind only generates what it finds in the source
const STOCK_CLASSES: Record<StockState, string> = {
  'in-stock': 'bg-emerald-50 text-emerald-700',
  low: 'bg-amber-50 text-amber-800',
  'sold-out': 'bg-slate-100 text-slate-500',
};

@Component({
  selector: 'app-book-card',
  imports: [CurrencyPipe],
  templateUrl: './book-card.html',
  styleUrl: './book-card.css',
})
export class BookCard {
  readonly book = input.required<Book>();

  protected readonly stock = computed<StockState>(() => {
    const count = this.book().stock;
    return count === 0 ? 'sold-out' : count < 5 ? 'low' : 'in-stock';
  });
  protected readonly stockClasses = computed(() => STOCK_CLASSES[this.stock()]);
  protected readonly soldOut = computed(() => this.stock() === 'sold-out');
}
