type CartSummaryProps = {
  itemCount: number;
};

export function CartSummary({ itemCount }: CartSummaryProps) {
  return <p className="muted">Cart: {itemCount} item(s)</p>;
}
