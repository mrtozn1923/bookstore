import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface CartItem {
  title: string;
  price: number;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private itemsSubject = new BehaviorSubject<CartItem[]>([]);
  readonly items$: Observable<CartItem[]> = this.itemsSubject.asObservable();
  readonly count$: Observable<number> = this.items$.pipe(map(items => items.length));

  add(item: CartItem): void {
    this.itemsSubject.next([...this.itemsSubject.value, item]);
  }

  clear(): void {
    this.itemsSubject.next([]);
  }

  getTotal(): number {
    return this.itemsSubject.value.reduce((sum, item) => sum + item.price, 0);
  }
}
