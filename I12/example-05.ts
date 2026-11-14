export function lineTotal(price: number, quantity: number) {
  return price * quantity;
}

export const freeShippingThreshold = 500;

export const cartSummary = {
  total: lineTotal(450, 2),
};

function normalizeIsbn(value: string) {
  return value.replaceAll("-", "");
}
