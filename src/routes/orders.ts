import { Router, type RequestHandler } from "express";
import type { OrderService } from "#services/orderService";
import { placeOrder, getOrder, exportOrders } from "#handlers/ordersHandlers";
import { requireRole } from "#middleware/auth";

// Every order endpoint needs a signed-in user; the export is for store staff only
// "/export" before "/:id", so "export" is never taken for an id
export function ordersRouter(orders: OrderService, authenticated: RequestHandler): Router {
    return Router()
        .use(authenticated)
        .post("/", placeOrder(orders))
        .get("/export", requireRole("StoreStaff"), exportOrders(orders))
        .get("/:id", getOrder(orders));
}
