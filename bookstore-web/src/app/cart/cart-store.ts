import { HttpClient } from '@angular/common/http';
import { computed, effect, inject } from '@angular/core';
import { patchState, signalStore, type, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import {
  addEntity,
  entityConfig,
  removeAllEntities,
  removeEntity,
  setAllEntities,
  updateEntity,
  withEntities,
} from '@ngrx/signals/entities';
import { firstValueFrom } from 'rxjs';
import { apiUrl } from '../api';
import type { Book, CartItem, Coupon } from '../models';
import { withRequestLog } from '../shared/with-request-log';
import { setError, setFulfilled, setPending, withRequestStatus } from '../shared/with-request-status';

// Cart lines as an entity collection: the ISBN is the id, '_' keeps the collection private
const lineConfig = entityConfig({
  entity: type<CartItem>(),
  collection: '_line',
  selectId: (line) => line.isbn,
});

export const CartStore = signalStore(
  { providedIn: 'root' },
  withEntities(lineConfig),
  withState({ coupon: null as Coupon | null }),
  withRequestStatus(),
  withRequestLog('cart'),
  withProps(() => ({
    _http: inject(HttpClient),
    _storageKey: 'bookstore.cart',
  })),
  withComputed(({ _lineEntities }) => ({
    // the public API stays the same: components still read cart.items()
    items: _lineEntities,
    count: computed(() => _lineEntities().reduce((sum, line) => sum + line.quantity, 0)),
    totalPrice: computed(() => _lineEntities().reduce((sum, line) => sum + line.price * line.quantity, 0)),
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
      // addEntity ignores an id that already exists, so an existing line is updated instead
      patchState(
        store,
        store._lineEntityMap()[book.isbn]
          ? updateEntity({ id: book.isbn, changes: (line) => ({ quantity: line.quantity + 1 }) }, lineConfig)
          : addEntity({ isbn: book.isbn, title: book.title, price: book.price, quantity: 1 }, lineConfig),
      );
    },
    setQuantity(isbn: string, quantity: number): void {
      patchState(
        store,
        quantity < 1 ? removeEntity(isbn, lineConfig) : updateEntity({ id: isbn, changes: { quantity } }, lineConfig),
      );
    },
    remove(isbn: string): void {
      patchState(store, removeEntity(isbn, lineConfig));
    },
    clear(): void {
      patchState(store, removeAllEntities(lineConfig), { coupon: null });
    },
    async applyCoupon(code: string): Promise<void> {
      patchState(store, setPending());
      try {
        const url = `${apiUrl}/coupons/${encodeURIComponent(code)}`;
        const coupon = await firstValueFrom(store._http.get<Coupon>(url));
        patchState(store, { coupon }, setFulfilled());
      } catch (error) {
        patchState(store, { coupon: null }, setError(error));
      }
    },
  })),
  withHooks({
    onInit(store) {
      const saved = readSavedCart(store._storageKey);
      patchState(store, setAllEntities(saved.items, lineConfig), { coupon: saved.coupon });
      // Save a versioned copy of the cart, not getState(): entityMap, ids and requestStatus are internal details
      effect(() => {
        const cart: SavedCart = { version: 2, items: store.items(), coupon: store.coupon() };
        localStorage.setItem(store._storageKey, JSON.stringify(cart));
      });
    },
  }),
);

type SavedCart = { version: 2; items: CartItem[]; coupon: Coupon | null };

// Reads every shape this app has ever saved: v0 array (posts 01-04), v1 object (05), v2 (06)
function readSavedCart(key: string): SavedCart {
  const empty: SavedCart = { version: 2, items: [], coupon: null };
  try {
    const saved: unknown = JSON.parse(localStorage.getItem(key) ?? 'null');
    if (Array.isArray(saved)) return { ...empty, items: saved as CartItem[] };
    if (saved && typeof saved === 'object' && Array.isArray((saved as SavedCart).items)) {
      const { items, coupon } = saved as SavedCart;
      return { version: 2, items, coupon: coupon ?? null };
    }
    return empty;
  } catch {
    return empty; // storage blocked or corrupt JSON: start with an empty cart
  }
}
