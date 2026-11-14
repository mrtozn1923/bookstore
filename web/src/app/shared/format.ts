const price = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' });

export function formatPrice(value: number): string {
  return price.format(value);
}
