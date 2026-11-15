import { useSuspenseQuery } from '@tanstack/react-query';
import { useParams } from 'react-router';
import { orderQuery } from '@/features/orders/orderQuery';
import { formatPrice } from '@/shared/format';

export function OrderPage() {
  const { orderId } = useParams();
  const { data: order } = useSuspenseQuery(orderQuery(orderId!));

  return (
    <>
      <h1>Order #{order.id}</h1>
      <p>Status: {order.status}</p>
      <ul>
        {order.items.map((item) => <li key={item.title}>{item.quantity} × {item.title} · {formatPrice(item.lineTotal)}</li>)}
      </ul>
      <p className="total">Total: {formatPrice(order.total)}</p>
    </>
  );
}
