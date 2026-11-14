import { HttpClient } from '@angular/common/http';
import { computed, effect, inject } from '@angular/core';
import {
  getState,
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withMethods,
  withProps,
  withState,
} from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { apiUrl } from '../api';
import type { Book, CartItem, Coupon } from '../models';

type CartState = {
  items: CartItem[];
  coupon: Coupon | null;
};

const initialState: CartState = { items: [], coupon: null };

// Global client state: one instance for the whole app (root injector)
export const CartStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  // Dependencies and constants; the _ prefix keeps them private to the store
  withProps(() => ({
    _http: inject(HttpClient),
    _storageKey: 'bookstore.cart',
  })),
  withComputed(({ items }) => ({
    count: computed(() => items().reduce((sum, item) => sum + item.quantity, 0)),
    totalPrice: computed(() => items().reduce((sum, item) => sum + item.price * item.quantity, 0)),
  })),
  withComputed(({ totalPrice, coupon }) => ({
    discount: computed(() => {
      const c = coupon();
      return c ? Math.round(totalPrice() * c.percent) / 100 : 0;
    }),
  })),
  withComputed(({ totalPrice, discount }) => ({
    payable: computed(() => totalPrice() - discount()),
  })),
  withMethods((store) => ({
    add(book: Book): void {
      patchState(store, ({ items }) => ({
        items: items.some((item) => item.isbn === book.isbn)
          ? items.map((item) =>
              item.isbn === book.isbn ? { ...item, quantity: item.quantity + 1 } : item,
            )
          : [...items, { isbn: book.isbn, title: book.title, price: book.price, quantity: 1 }],
      }));
    },
    setQuantity(isbn: string, quantity: number): void {
      patchState(store, ({ items }) => ({
        items:
          quantity < 1
            ? items.filter((item) => item.isbn !== isbn)
            : items.map((item) => (item.isbn === isbn ? { ...item, quantity } : item)),
      }));
    },
    remove(isbn: string): void {
      patchState(store, ({ items }) => ({ items: items.filter((item) => item.isbn !== isbn) }));
    },
    clear(): void {
      patchState(store, initialState);
    },
    async applyCoupon(code: string): Promise<boolean> {
      try {
        const url = `${apiUrl}/coupons/${encodeURIComponent(code)}`;
        const coupon = await firstValueFrom(store._http.get<Coupon>(url));
        patchState(store, { coupon });
        return true;
      } catch {
        patchState(store, { coupon: null });
        return false;
      }
    },
  })),
  withHooks({
    onInit(store) {
      // Hydrate once from localStorage, then save every state change (outward sync only)
      patchState(store, readSavedCart(store._storageKey));
      effect(() => localStorage.setItem(store._storageKey, JSON.stringify(getState(store))));
    },
  }),
);

function readSavedCart(key: string): Partial<CartState> {
  try {
    const saved = JSON.parse(localStorage.getItem(key) ?? '{}') as Partial<CartState>;
    return Array.isArray(saved.items) ? { items: saved.items, coupon: saved.coupon ?? null } : {};
  } catch {
    return {}; // storage blocked or corrupt JSON: start with an empty cart
  }
}
