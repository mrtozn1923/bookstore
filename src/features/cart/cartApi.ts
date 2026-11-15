import type { Cart, Order } from '@/features/cart/types';
import { apiFetch } from '@/shared/api';

export const cartKey = (cartId: string | null) => ['cart', cartId] as const;

export const cartApi = {
  create: () => apiFetch<Cart>('/api/carts', { method: 'POST' }),
  get: (cartId: string, signal?: AbortSignal) => apiFetch<Cart>(`/api/carts/${cartId}`, { signal }),
  setQuantity: (cartId: string, isbn: string, quantity: number) =>
    apiFetch<Cart>(`/api/carts/${cartId}/items/${isbn}`, { method: 'PUT', body: JSON.stringify({ quantity }) }),
  remove: (cartId: string, isbn: string) => apiFetch<Cart>(`/api/carts/${cartId}/items/${isbn}`, { method: 'DELETE' }),
  placeOrder: (cartId: string, customerName: string, customerEmail: string) =>
    apiFetch<Order>('/api/orders', { method: 'POST', body: JSON.stringify({ cartId, customerName, customerEmail }) }),
  getOrder: (orderId: string, signal?: AbortSignal) => apiFetch<Order>(`/api/orders/${orderId}`, { signal }),
};
