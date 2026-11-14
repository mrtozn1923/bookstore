export interface CartItem {
  isbn: string;
  price: number;
  quantity: number;
}

const TAX_RATE = 0.2;

export function cartTotal(items: readonly CartItem[]) {
  const subtotal = items.reduce((sum, item) => sum + item.price * item.quantity, 0);
  return Math.round(subtotal * (1 + TAX_RATE) * 100) / 100;
}

export class Cart {
  #items: CartItem[] = [];

  add(item: CartItem): this {
    this.#items.push(item);
    return this;
  }

  get total() {
    return cartTotal(this.#items);
  }
}
