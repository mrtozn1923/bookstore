import { redirect, type ActionFunctionArgs } from 'react-router';
import { userContext } from '@/features/auth/requireUser';
import { cartApi } from '@/features/cart/cartApi';
import { cartIdStore } from '@/features/cart/cartIdStore';
import { ApiError } from '@/shared/api';
import { queryClient } from '@/shared/queryClient';

// Runs when <Form method="post"> is submitted; requireUser has already put the user into the context
export async function checkoutAction({ context }: ActionFunctionArgs) {
  const user = context.get(userContext);
  const cartId = cartIdStore.get();
  if (cartId === null) {
    return { error: 'Your cart is empty.' };
  }
  try {
    const order = await cartApi.placeOrder(cartId, user.name, user.email);
    cartIdStore.set(null);
    queryClient.removeQueries({ queryKey: ['cart'] });
    queryClient.invalidateQueries({ queryKey: ['book'] });
    return redirect(`/orders/${order.id}`);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.message };
    }
    throw error;
  }
}
