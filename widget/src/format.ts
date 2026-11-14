const priceFormat = new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY" });

export function formatPrice(value: number): string {
    return priceFormat.format(value);
}

export function formatIsbn(isbn: string): string {
    return `${isbn.slice(0, 3)}-${isbn.slice(3, 4)}-${isbn.slice(4, 7)}-${isbn.slice(7, 12)}-${isbn.slice(12)}`;
}
