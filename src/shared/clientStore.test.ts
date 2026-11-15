import { expect, it } from 'vitest';
import { migrateLegacyCartId, useClientStore } from '@/shared/clientStore';

it('moves the old bookstore.cartId key into the persisted store once', async () => {
  localStorage.setItem('bookstore.cartId', 'cart-from-post-10');

  migrateLegacyCartId(localStorage);
  await useClientStore.persist.rehydrate();

  expect(useClientStore.getState().cartId).toBe('cart-from-post-10');
  expect(localStorage.getItem('bookstore.cartId')).toBeNull();
});

it('keeps the newer store when both keys exist', () => {
  localStorage.setItem('bookstore.client', JSON.stringify({ state: { cartId: 'new-cart', recentlyViewed: [] }, version: 1 }));
  localStorage.setItem('bookstore.cartId', 'old-cart');

  migrateLegacyCartId(localStorage);

  expect(JSON.parse(localStorage.getItem('bookstore.client')!).state.cartId).toBe('new-cart');
  expect(localStorage.getItem('bookstore.cartId')).toBeNull();
});
