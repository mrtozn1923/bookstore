import { Router } from "express";
import type { BookService } from "#services/bookService";
import type { OrderService } from "#services/orderService";
import { placeOrder, getOrder, exportOrders } from "#handlers/ordersHandlers";

// "/export" before "/:id", so "export" is never taken for an id
export function ordersRouter(orders: OrderService, books: BookService): Router {
    return Router().post("/", placeOrder(orders)).get("/export", exportOrders(books)).get("/:id", getOrder(orders));
}
