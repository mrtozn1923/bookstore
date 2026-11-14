import { Injectable } from '@angular/core';

export interface CartItem {
  title: string;
  price: number;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private items: CartItem[] = [];

  add(item: CartItem): void {
    this.items.push(item);
  }

  clear(): void {
    this.items = [];
  }

  get count(): number {
    return this.items.length;
  }

  get total(): number {
    return this.items.reduce((sum, item) => sum + item.price, 0);
  }
}
