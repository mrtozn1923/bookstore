import { redirect } from 'react-router';
import { cartQuery } from '@/features/cart/cartApi';
import { cartIdStore } from '@/features/cart/cartIdStore';
import { queryClient } from '@/shared/queryClient';

// The form needs the cart lines as default values, so they must be loaded before it renders
export async function checkoutLoader() {
  const cartId = cartIdStore.get();
  const cart = cartId === null ? null : await queryClient.ensureQueryData(cartQuery(cartId));
  if (cart === null || cart.items.length === 0) {
    throw redirect('/cart');
  }
  return null;
}
