import type sql from "mssql";
import { SqlBookRepository } from "#repositories/sqlBookRepository";
import { SqlOrderRepository } from "#repositories/sqlOrderRepository";
import { BookService } from "#services/bookService";
import { OrderService } from "#services/orderService";
import { registerOrderNotifications } from "#services/notificationService";
import type { AppServices } from "./app.ts";

// Composition root: the one place that decides which implementations are used
export function createServices(pool: sql.ConnectionPool): Omit<AppServices, "readiness" | "carrier" | "paymentEvents" | "catalogParser"> {
    const bookRepository = new SqlBookRepository(pool);
    const orderService = new OrderService(bookRepository, new SqlOrderRepository(pool));
    registerOrderNotifications(orderService);
    return { books: new BookService(bookRepository), orders: orderService };
}
