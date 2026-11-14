export type Currency = "TRY" | "EUR";

export interface Book {
    isbn: string;
    title: string;
    author: string;
    price: number;
    currency: Currency;
}
