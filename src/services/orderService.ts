import { EventEmitter } from "node:events";
import type { Readable } from "node:stream";
import type { Order, OrderItem } from "#domain/order";
import { MixedCurrencyError, OrderNotFoundError, UnknownBookError } from "#domain/errors";
import type { BookRepository } from "#repositories/bookRepository";
import type { OrderRepository } from "#repositories/orderRepository";
import type { Principal } from "../auth/tokenVerifier.ts";
import { logger } from "../diagnostics/logger.ts";

export interface OrderLine {
    isbn: string;
    quantity: number;
}

interface OrderEvents {
    orderPlaced: [order: Order];
    error: [err: Error];
}

// Places orders and announces them; listeners (notifications, reports) stay decoupled
export class OrderService extends EventEmitter<OrderEvents> {
    readonly #books: BookRepository;
    readonly #orders: OrderRepository;

    constructor(books: BookRepository, orders: OrderRepository) {
        // Rejections from async listeners go to the 'error' event instead of crashing the process
        super({ captureRejections: true });
        this.#books = books;
        this.#orders = orders;
        this.on("error", (err) => {
            logger.error({ error: `${err.name}: ${err.message}`, cause: (err.cause as Error | undefined)?.name }, "orderPlaced listener failed");
        });
    }

    async placeOrder(customerId: string, lines: readonly OrderLine[]): Promise<Order> {
        const books = await this.#books.findManyByIsbn(lines.map((l) => l.isbn));
        const unknown = lines.filter((l) => !books.has(l.isbn)).map((l) => l.isbn);
        if (unknown.length > 0) {
            throw new UnknownBookError(unknown);
        }
        const currencies = new Set(lines.map((l) => books.get(l.isbn)!.currency));
        if (currencies.size > 1) {
            throw new MixedCurrencyError();
        }
        // The price is taken from the catalog, never from the request
        const items: OrderItem[] = lines.map((l) => ({ isbn: l.isbn, quantity: l.quantity, unitPriceMinor: books.get(l.isbn)!.priceMinor }));
        const order: Order = {
            id: await this.#orders.nextId(),
            customerId,
            status: "Pending",
            currency: [...currencies][0],
            items,
            createdAtUtc: new Date().toISOString(),
        };
        await this.#orders.add(order);
        logger.info({ orderId: order.id, items: items.length }, "order placed");
        this.emit("orderPlaced", order);
        return order;
    }

    // Object-mode stream of OrderExportRow, straight from the repository
    exportRows(): Readable {
        return this.#orders.exportRows();
    }

    // Object-level authorization: customers see their own orders, store staff see all of them
    async getForUser(id: string, user: Principal): Promise<Order> {
        const order = await this.#orders.findById(id);
        // Someone else's order is "not found", not "forbidden": the answer must not confirm that it exists
        if (!order || (order.customerId !== user.userId && !user.roles.includes("StoreStaff"))) {
            throw new OrderNotFoundError(id);
        }
        return order;
    }
}
