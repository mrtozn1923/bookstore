const tryCurrency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'TRY', currencyDisplay: 'narrowSymbol' });

export function formatPrice(value: number): string {
  return tryCurrency.format(value);
}
