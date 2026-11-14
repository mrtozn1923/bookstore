import { Readable } from "node:stream";
import type { Book } from "#domain/book";
import type { Order } from "#domain/order";
import { OutOfStockError } from "#domain/errors";
import type { OrderExportRow } from "#services/orderExport";

export interface OrderRepository {
    nextId(): Promise<string>;
    // Saves the order and takes its lines out of stock; throws OutOfStockError and saves nothing if a line does not fit
    add(order: Order): Promise<void>;
    findById(id: string): Promise<Order | undefined>;
    // Every order line, oldest first, as an object-mode stream of OrderExportRow
    exportRows(): Readable;
}

// Lives as long as the process; the SQL Server implementation is SqlOrderRepository
export class InMemoryOrderRepository implements OrderRepository {
    readonly #orders = new Map<string, Order>();
    readonly #books: ReadonlyMap<string, Book>;
    readonly #stock: Map<string, number>;
    #nextNumber = 1001;

    constructor(books: readonly Book[] = [], stock: ReadonlyMap<string, number> = new Map()) {
        this.#books = new Map(books.map((b) => [b.isbn, b]));
        this.#stock = new Map(stock);
    }

    async nextId(): Promise<string> {
        return `ORD-${this.#nextNumber++}`;
    }

    async add(order: Order): Promise<void> {
        const missing = order.items.filter((i) => (this.#stock.get(i.isbn) ?? 0) < i.quantity).map((i) => i.isbn);
        if (missing.length > 0) {
            throw new OutOfStockError(missing);
        }
        for (const item of order.items) {
            this.#stock.set(item.isbn, this.#stock.get(item.isbn)! - item.quantity);
        }
        this.#orders.set(order.id, order);
    }

    async findById(id: string): Promise<Order | undefined> {
        return this.#orders.get(id);
    }

    exportRows(): Readable {
        const rows = [...this.#orders.values()].flatMap((order) =>
            order.items.map(
                (item): OrderExportRow => ({
                    orderId: order.id,
                    createdAtUtc: order.createdAtUtc,
                    customerId: order.customerId,
                    isbn: item.isbn,
                    title: this.#books.get(item.isbn)?.title ?? "",
                    quantity: item.quantity,
                    unitPriceMinor: item.unitPriceMinor,
                    currency: order.currency,
                    status: order.status,
                }),
            ),
        );
        return Readable.from(rows);
    }
}
