import { setTimeout as delay } from "node:timers/promises";
import type { Order } from "#domain/order";
import type { OrderService } from "#services/orderService";
import { logger } from "../diagnostics/logger.ts";

// Stand-in for an e-mail provider call; honours the abort signal like fetch does
async function sendEmail(to: string, subject: string, signal: AbortSignal): Promise<void> {
    await delay(Number(process.env.EMAIL_LATENCY_MS ?? 30), undefined, { signal });
    logger.info({ to, subject }, "e-mail sent");
}

async function sendOrderConfirmation(order: Order): Promise<void> {
    await sendEmail(`${order.customerId}@example.com`, `Order ${order.id} received`, AbortSignal.timeout(2000));
}

export function registerOrderNotifications(orders: OrderService): void {
    orders.on("orderPlaced", sendOrderConfirmation);
}
