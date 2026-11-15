import { useCart } from '@/features/cart/useCart';
import { formatPrice } from '@/shared/format';

export function CartSummary() {
  const { itemCount, total } = useCart();
  return <span className="cart-summary">Cart: {itemCount} item(s) · {formatPrice(total)}</span>;
}
