import type { Book } from "#domain/book";
import books from "#db/books.json" with { type: "json" };
import { InMemoryBookRepository } from "#repositories/bookRepository";
import { InMemoryOrderRepository } from "#repositories/orderRepository";
import { BookService } from "#services/bookService";
import { OrderService } from "#services/orderService";
import { registerOrderNotifications } from "#services/notificationService";
import type { AppServices } from "./app.ts";

// Composition root: the one place that decides which implementations are used
export function createServices(): AppServices {
    const bookRepository = new InMemoryBookRepository(books as Book[]);
    const orderService = new OrderService(bookRepository, new InMemoryOrderRepository());
    registerOrderNotifications(orderService);
    return { books: new BookService(bookRepository), orders: orderService };
}
