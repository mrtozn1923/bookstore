import type { Order } from "#domain/order";

export interface OrderRepository {
    nextId(): Promise<string>;
    add(order: Order): Promise<void>;
    findById(id: string): Promise<Order | undefined>;
}

// Lives as long as the process; replaced by SQL Server in post 13
export class InMemoryOrderRepository implements OrderRepository {
    readonly #orders = new Map<string, Order>();
    #nextNumber = 1001;

    async nextId(): Promise<string> {
        return `ORD-${this.#nextNumber++}`;
    }

    async add(order: Order): Promise<void> {
        this.#orders.set(order.id, order);
    }

    async findById(id: string): Promise<Order | undefined> {
        return this.#orders.get(id);
    }
}
