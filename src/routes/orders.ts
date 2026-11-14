import { Router } from "express";
import type { OrderService } from "#services/orderService";
import { placeOrder, getOrder, exportOrders } from "#handlers/ordersHandlers";

// "/export" before "/:id", so "export" is never taken for an id
export function ordersRouter(orders: OrderService): Router {
    return Router().post("/", placeOrder(orders)).get("/export", exportOrders(orders)).get("/:id", getOrder(orders));
}
