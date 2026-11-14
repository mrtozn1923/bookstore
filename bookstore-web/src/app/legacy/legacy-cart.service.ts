import { Injectable } from '@angular/core';
import { BehaviorSubject, map, type Observable } from 'rxjs';
import type { Book, CartItem } from '../models';

// The cart as an Angular 12 era app would write it; kept only for comparison (post 10), the app does not use it
@Injectable({ providedIn: 'root' })
export class LegacyCartService {
  private readonly itemsSubject = new BehaviorSubject<CartItem[]>([]);
  readonly items$: Observable<CartItem[]> = this.itemsSubject.asObservable();
  readonly count$ = this.items$.pipe(map((items) => items.reduce((sum, item) => sum + item.quantity, 0)));

  add(book: Book): void {
    const items = this.itemsSubject.value;
    const line = items.find((item) => item.isbn === book.isbn);
    this.itemsSubject.next(
      line
        ? items.map((item) => (item === line ? { ...item, quantity: item.quantity + 1 } : item))
        : [...items, { isbn: book.isbn, title: book.title, price: book.price, quantity: 1 }],
    );
  }
}
