import type { OrderStatus } from "@bookstore/contracts";
import type { Book } from "./book.ts";

export interface OrderItem {
    isbn: Book["isbn"];
    quantity: number;
    unitPrice: number;
}

export interface Order {
    id: string;
    customerId: string;
    status: OrderStatus;
    items: OrderItem[];
    createdAtUtc: string;
}

export function orderTotal(order: Order): number {
    return order.items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);
}
