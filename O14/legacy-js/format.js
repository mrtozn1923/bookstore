export function formatPrice(amount, currency) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency }).format(amount);
}

export const DEFAULT_CURRENCY = "TRY";
