type CartSummaryProps = {
  itemCount: number;
};

export function CartSummary({ itemCount }: CartSummaryProps) {
  return <span className="cart-summary">Cart: {itemCount} item(s)</span>;
}
