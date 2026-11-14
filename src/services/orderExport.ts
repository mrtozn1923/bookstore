import { Readable, Transform } from "node:stream";
import type { Book } from "#domain/book";
import { formatAmount } from "#domain/money";
import { ORDER_STATUSES } from "@bookstore/contracts";

export interface OrderExportRow {
    orderId: string;
    createdAtUtc: string;
    customerId: string;
    isbn: string;
    title: string;
    quantity: number;
    unitPriceMinor: number;
    currency: string;
    status: string;
}

const HEADER = "orderId,createdAtUtc,customerId,isbn,title,quantity,unitPrice,currency,status\n";
const START = Date.UTC(2026, 0, 1);

// Synthetic order lines until the SQL Server post; generated lazily, one row at a time
export function* generateOrderRows(count: number, books: readonly Book[]): Generator<OrderExportRow> {
    for (let i = 0; i < count; i++) {
        const book = books[i % books.length];
        yield {
            orderId: `ORD-${100001 + i}`,
            createdAtUtc: new Date(START + i * 7_000).toISOString(),
            customerId: `customer-${String(i % 50_000).padStart(5, "0")}`,
            isbn: book.isbn,
            title: book.title,
            quantity: (i % 3) + 1,
            unitPriceMinor: book.priceMinor,
            currency: book.currency,
            status: ORDER_STATUSES[i % ORDER_STATUSES.length],
        };
    }
}

// RFC 4180 quoting: wrap in quotes when the value contains a comma, quote or line break
function csvField(value: string | number): string {
    const text = String(value);
    return /[",\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
}

export function toCsvLine(row: OrderExportRow): string {
    return [row.orderId, row.createdAtUtc, row.customerId, row.isbn, row.title, row.quantity, formatAmount(row.unitPriceMinor), row.currency, row.status]
        .map(csvField)
        .join(",") + "\n";
}

// Object mode in, text out; batches lines so each chunk is a few KB instead of one tiny string per row
export function csvTransform(batchSize = 200): Transform {
    let batch: string[] = [HEADER];
    return new Transform({
        writableObjectMode: true,
        transform(row: OrderExportRow, encoding, callback) {
            batch.push(toCsvLine(row));
            if (batch.length >= batchSize) {
                const chunk = batch.join("");
                batch = [];
                callback(null, chunk);
            } else {
                callback();
            }
        },
        flush(callback) {
            callback(null, batch.join(""));
        },
    });
}

export function orderRowStream(count: number, books: readonly Book[]): Readable {
    return Readable.from(generateOrderRows(count, books));
}
