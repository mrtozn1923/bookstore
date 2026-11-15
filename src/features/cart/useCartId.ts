import { useSyncExternalStore } from 'react';
import { cartIdStore } from '@/features/cart/cartIdStore';

export function useCartId() {
  const cartId = useSyncExternalStore(cartIdStore.subscribe, cartIdStore.get);
  return { cartId, setCartId: cartIdStore.set };
}
