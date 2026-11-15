import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CartView, OrderView } from '../core/api-models';

const cartIdKey = 'bookstore.cartId';

// Keeps the visitor's cart; the cart itself lives in the API, the browser only remembers its id.
@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly http = inject(HttpClient);
  private readonly cartState = signal<CartView | null>(null);

  readonly cart = this.cartState.asReadonly();
  readonly itemCount = computed(() =>
    (this.cartState()?.items ?? []).reduce((sum, line) => sum + line.quantity, 0));

  async load(): Promise<void> {
    const id = localStorage.getItem(cartIdKey);
    if (!id) {
      return;
    }
    try {
      this.cartState.set(await firstValueFrom(this.http.get<CartView>(`/api/carts/${id}`)));
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        localStorage.removeItem(cartIdKey);
        this.cartState.set(null);
        return;
      }
      throw error;
    }
  }

  async setQuantity(isbn: string, quantity: number): Promise<void> {
    const cartId = await this.ensureCart();
    this.cartState.set(await firstValueFrom(
      this.http.put<CartView>(`/api/carts/${cartId}/items/${isbn}`, { quantity })));
  }

  async add(isbn: string, quantity = 1): Promise<void> {
    const current = this.cartState()?.items.find(line => line.isbn === isbn)?.quantity ?? 0;
    await this.setQuantity(isbn, current + quantity);
  }

  async remove(isbn: string): Promise<void> {
    const cartId = this.cartState()?.id;
    if (cartId) {
      this.cartState.set(await firstValueFrom(this.http.delete<CartView>(`/api/carts/${cartId}/items/${isbn}`)));
    }
  }

  async placeOrder(customerName: string, customerEmail: string): Promise<OrderView> {
    const cartId = this.cartState()?.id;
    if (!cartId) {
      throw new Error('The cart is empty.');
    }
    const order = await firstValueFrom(
      this.http.post<OrderView>('/api/orders', { cartId, customerName, customerEmail }));
    localStorage.removeItem(cartIdKey);
    this.cartState.set(null);
    return order;
  }

  private async ensureCart(): Promise<string> {
    const existing = this.cartState()?.id;
    if (existing) {
      return existing;
    }
    const created = await firstValueFrom(this.http.post<CartView>('/api/carts', null));
    localStorage.setItem(cartIdKey, created.id);
    this.cartState.set(created);
    return created.id;
  }
}
