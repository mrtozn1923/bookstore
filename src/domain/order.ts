import type { OrderStatus } from "@bookstore/contracts";
import type { Book } from "./book.ts";
import type { Currency } from "./money.ts";

export interface OrderItem {
    isbn: Book["isbn"];
    quantity: number;
    unitPriceMinor: number;
}

export interface Order {
    id: string;
    customerId: string;
    status: OrderStatus;
    currency: Currency;
    items: OrderItem[];
    createdAtUtc: string;
}

// Integer arithmetic: exact as long as totals stay below Number.MAX_SAFE_INTEGER kuruş
export function orderTotalMinor(order: Order): number {
    return order.items.reduce((sum, item) => sum + item.quantity * item.unitPriceMinor, 0);
}
