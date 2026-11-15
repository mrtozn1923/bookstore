import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface CartItem {
  title: string;
  price: number;
}

export interface CartLine extends CartItem {
  lineId: number;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private nextLineId = 1;
  private itemsSubject = new BehaviorSubject<CartLine[]>([]);
  readonly items$: Observable<CartLine[]> = this.itemsSubject.asObservable();
  readonly count$: Observable<number> = this.items$.pipe(map(items => items.length));

  add(item: CartItem): void {
    const line: CartLine = { title: item.title, price: item.price, lineId: this.nextLineId++ };
    this.itemsSubject.next([...this.itemsSubject.value, line]);
  }

  remove(lineId: number): void {
    this.itemsSubject.next(this.itemsSubject.value.filter(line => line.lineId !== lineId));
  }

  clear(): void {
    this.itemsSubject.next([]);
  }

  getTotal(): number {
    return this.itemsSubject.value.reduce((sum, item) => sum + item.price, 0);
  }
}
