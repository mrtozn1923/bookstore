import { redirect } from 'react-router';
import { cartQuery } from '@/features/cart';
import { useClientStore } from '@/shared/clientStore';
import { queryClient } from '@/shared/queryClient';

// The form needs the cart lines as default values, so they must be loaded before it renders
export async function checkoutLoader() {
  const cartId = useClientStore.getState().cartId;
  const cart = cartId === null ? null : await queryClient.ensureQueryData(cartQuery(cartId));
  if (cart === null || cart.items.length === 0) {
    throw redirect('/cart');
  }
  return null;
}
