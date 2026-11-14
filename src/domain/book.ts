import type { Currency } from "./money.ts";

export type { Currency } from "./money.ts";

export interface Book {
    isbn: string;
    title: string;
    author: string;
    // Integer minor units: 34990 means 349.90
    priceMinor: number;
    currency: Currency;
}
