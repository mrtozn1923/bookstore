import type { LoaderFunctionArgs } from 'react-router';
import { orderQuery } from './orderQuery';
import { queryClient } from '@/shared/queryClient';

export async function orderLoader({ params }: LoaderFunctionArgs) {
  await queryClient.ensureQueryData(orderQuery(params.orderId!));
  return null;
}
