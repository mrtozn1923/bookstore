import { formatPrice } from '@/shared/format';

type CartSummaryProps = {
  itemCount: number;
  total: number;
};

export function CartSummary({ itemCount, total }: CartSummaryProps) {
  return <span className="cart-summary">Cart: {itemCount} item(s) · {formatPrice(total)}</span>;
}
