import { useClientStore } from '@/shared/clientStore';

export function useCartId() {
  // Two selectors: this component re-renders only when cartId changes, not on every store update
  const cartId = useClientStore((state) => state.cartId);
  const setCartId = useClientStore((state) => state.setCartId);
  return { cartId, setCartId };
}
