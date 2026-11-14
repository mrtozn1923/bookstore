// Error: An implementation cannot be declared in ambient contexts.
export function lineTotal(price: number, quantity: number): number {
  return price * quantity;
}
