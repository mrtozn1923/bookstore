export const SHIPPING_FEE = 40;

export function orderTotal(prices) {
  return prices.reduce((sum, price) => sum + price, 0) + SHIPPING_FEE;
}
