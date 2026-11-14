import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { OrderNotFoundError, OutOfStockError, UnknownBookError } from "#domain/errors";
import type { Order } from "#domain/order";
import { createTestServices, DUNE, EMMA } from "../helpers.ts";

describe("OrderService.placeOrder", () => {
    it("takes the price from the catalog and announces the order", async (t) => {
        const { orders } = createTestServices();
        // A mock function as the listener: it records every call
        const listener = t.mock.fn((order: Order) => {});
        orders.on("orderPlaced", listener);

        const order = await orders.placeOrder("ada", [{ isbn: DUNE, quantity: 2 }]);

        assert.deepEqual(order.items, [{ isbn: DUNE, quantity: 2, unitPriceMinor: 34990 }]);
        assert.equal(listener.mock.callCount(), 1);
        assert.equal(listener.mock.calls[0].arguments[0].id, order.id);
    });

    it("rejects an order that does not fit the stock and announces nothing", async (t) => {
        const { orders, orderRepository } = createTestServices({ [DUNE]: 1, [EMMA]: 5 });
        const listener = t.mock.fn();
        orders.on("orderPlaced", listener);
        // Spy on the repository method; the real implementation still runs
        const add = t.mock.method(orderRepository, "add");

        await assert.rejects(orders.placeOrder("ada", [{ isbn: EMMA, quantity: 1 }, { isbn: DUNE, quantity: 2 }]), (err: unknown) => {
            assert.ok(err instanceof OutOfStockError);
            assert.deepEqual(err.isbns, [DUNE]);
            return true;
        });
        assert.equal(add.mock.callCount(), 1);
        assert.equal(listener.mock.callCount(), 0);
        // Nothing was taken out of stock: the next order for the last copy still succeeds
        await orders.placeOrder("alan", [{ isbn: DUNE, quantity: 1 }]);
    });

    it("rejects an unknown ISBN before touching the order repository", async (t) => {
        const { orders, orderRepository } = createTestServices();
        const add = t.mock.method(orderRepository, "add");

        await assert.rejects(orders.placeOrder("ada", [{ isbn: "9780306406157", quantity: 1 }]), UnknownBookError);
        assert.equal(add.mock.callCount(), 0);
    });
});

describe("OrderService.getForUser", () => {
    it("shows an order to its owner and to store staff only", async () => {
        const { orders } = createTestServices();
        const order = await orders.placeOrder("ada", [{ isbn: DUNE, quantity: 1 }]);

        assert.equal((await orders.getForUser(order.id, { userId: "ada", roles: ["Customer"] })).id, order.id);
        assert.equal((await orders.getForUser(order.id, { userId: "grace", roles: ["StoreStaff"] })).id, order.id);
        await assert.rejects(orders.getForUser(order.id, { userId: "alan", roles: ["Customer"] }), OrderNotFoundError);
    });
});
