type FixedDiscount = { amount: number };
type PercentDiscount = { percent: number };

function applyDiscount(price: number, discount: FixedDiscount | PercentDiscount) {
  // Error: Property 'percent' does not exist on type 'FixedDiscount | PercentDiscount'.
  return price - (price * discount.percent) / 100;
}
