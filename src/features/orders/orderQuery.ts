import { queryOptions } from '@tanstack/react-query';
import { cartApi } from '@/features/cart';

export const orderQuery = (orderId: string) =>
  queryOptions({ queryKey: ['order', orderId], queryFn: ({ signal }) => cartApi.getOrder(orderId, signal) });
