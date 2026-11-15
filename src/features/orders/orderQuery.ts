import { queryOptions } from '@tanstack/react-query';
import { cartApi } from '@/features/cart/cartApi';

export const orderQuery = (orderId: string) =>
  queryOptions({ queryKey: ['order', orderId], queryFn: ({ signal }) => cartApi.getOrder(orderId, signal) });
