import { toBookSummary, type BookSummary } from "@bookstore/contracts";
import type { Book } from "#domain/book";
import { toAmount } from "#domain/money";
import { orderTotalMinor, type Order } from "#domain/order";

// Response shapes: what the Angular client sees; the domain types never leave the API as they are
export interface MoneyDto {
    amount: number;
    currency: string;
}

export interface OrderDto {
    id: string;
    customerId: string;
    status: string;
    createdAtUtc: string;
    items: { isbn: string; quantity: number; unitPrice: MoneyDto; lineTotal: MoneyDto }[];
    total: MoneyDto;
}

export function toBookDto(book: Book): BookSummary {
    return toBookSummary(book);
}

export function toOrderDto(order: Order): OrderDto {
    const money = (minor: number): MoneyDto => ({ amount: toAmount(minor), currency: order.currency });
    return {
        id: order.id,
        customerId: order.customerId,
        status: order.status,
        createdAtUtc: order.createdAtUtc,
        items: order.items.map((i) => ({ isbn: i.isbn, quantity: i.quantity, unitPrice: money(i.unitPriceMinor), lineTotal: money(i.quantity * i.unitPriceMinor) })),
        total: money(orderTotalMinor(order)),
    };
}
