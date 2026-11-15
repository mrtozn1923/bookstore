import { queryOptions } from '@tanstack/react-query';
import type { Cart, Order } from '@/features/cart/types';
import { ApiError, apiFetch } from '@/shared/api';

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

// Shared by useCart and the checkout loader
export function cartQuery(cartId: string | null) {
  return queryOptions({
    queryKey: cartKey(cartId),
    queryFn: async ({ signal }) => {
      try {
        return await cartApi.get(cartId!, signal);
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) {
          return null; // the server forgot this cart (e.g. it was ordered)
        }
        throw error;
      }
    },
  });
}
