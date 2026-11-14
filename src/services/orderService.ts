import { EventEmitter } from "node:events";
import type { Order, OrderItem } from "#domain/order";
import { findBook } from "#services/bookService";
import { log } from "../diagnostics/log.ts";

export interface OrderLine {
    isbn: string;
    quantity: number;
}

interface OrderEvents {
    orderPlaced: [order: Order];
    error: [err: Error];
}

export class UnknownBookError extends Error {
    readonly isbn: string;

    constructor(isbn: string) {
        super(`unknown ISBN ${isbn}`);
        this.isbn = isbn;
        this.name = "UnknownBookError";
    }
}

// Places orders in memory and announces them; listeners (notifications, reports) stay decoupled
export class OrderService extends EventEmitter<OrderEvents> {
    #orders = new Map<string, Order>();
    #nextNumber = 1001;

    constructor() {
        // Rejections from async listeners go to the 'error' event instead of crashing the process
        super({ captureRejections: true });
        this.on("error", (err) => {
            log("error", "orderPlaced listener failed", { error: `${err.name}: ${err.message}`, cause: (err.cause as Error | undefined)?.name });
        });
    }

    placeOrder(customerId: string, lines: readonly OrderLine[]): Order {
        const items: OrderItem[] = lines.map((line) => {
            const book = findBook(line.isbn);
            if (!book) {
                throw new UnknownBookError(line.isbn);
            }
            return { isbn: book.isbn, quantity: line.quantity, unitPrice: book.price };
        });
        const order: Order = {
            id: `ORD-${this.#nextNumber++}`,
            customerId,
            status: "Pending",
            items,
            createdAtUtc: new Date().toISOString(),
        };
        this.#orders.set(order.id, order);
        log("info", "order placed", { orderId: order.id, items: items.length });
        this.emit("orderPlaced", order);
        return order;
    }

    findOrder(id: string): Order | undefined {
        return this.#orders.get(id);
    }
}

export const orderService = new OrderService();
