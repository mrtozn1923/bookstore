import { useMutation, useQueryClient } from '@tanstack/react-query';
import { cartApi } from '@/features/cart/cartApi';
import { useCartId } from '@/features/cart/CartContext';

export function usePlaceOrder() {
  const { setCartId } = useCartId();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: { cartId: string; name: string; email: string }) =>
      cartApi.placeOrder(input.cartId, input.name, input.email),
    onSuccess: () => {
      // The server deleted the cart and reserved stock: forget the cart, refetch stock numbers
      setCartId(null);
      queryClient.removeQueries({ queryKey: ['cart'] });
      queryClient.invalidateQueries({ queryKey: ['book'] });
    },
  });
}
