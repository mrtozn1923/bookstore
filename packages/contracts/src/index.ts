// Shapes and rules shared by the API and other Bookstore Node.js packages
export const ORDER_STATUSES = ["Pending", "Paid", "Shipped", "Delivered", "Cancelled"] as const;
export type OrderStatus = (typeof ORDER_STATUSES)[number];

export interface BookSummary {
    isbn: string;
    title: string;
    price: { amount: number; currency: string };
}

export function isValidIsbn13(isbn: string): boolean {
    if (!/^97[89]\d{10}$/.test(isbn)) {
        return false;
    }
    const sum = [...isbn].reduce((acc, ch, i) => acc + Number(ch) * (i % 2 === 0 ? 1 : 3), 0);
    return sum % 10 === 0;
}

export function toBookSummary(book: { isbn: string; title: string; price: number; currency: string }): BookSummary {
    return {
        isbn: book.isbn,
        title: book.title,
        price: { amount: book.price, currency: book.currency },
    };
}
