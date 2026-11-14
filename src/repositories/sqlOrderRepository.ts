import sql from "mssql";
import { pipeline, Transform, type Readable } from "node:stream";
import type { OrderStatus } from "@bookstore/contracts";
import type { Currency } from "#domain/money";
import { toAmount } from "#domain/money";
import type { Order } from "#domain/order";
import { OutOfStockError } from "#domain/errors";
import type { OrderRepository } from "#repositories/orderRepository";
import type { OrderExportRow } from "#services/orderExport";

interface OrderRow {
    Id: number;
    CustomerId: string;
    Status: OrderStatus;
    Currency: Currency;
    CreatedAtUtc: Date;
}

interface OrderItemRow {
    Isbn: string;
    Quantity: number;
    UnitPriceMinor: number;
}

// "ORD-1001" in the API, 1001 in the database
const toOrderId = (id: number): string => `ORD-${id}`;
const fromOrderId = (id: string): number | undefined => (/^ORD-\d{1,9}$/.test(id) ? Number(id.slice(4)) : undefined);

export class SqlOrderRepository implements OrderRepository {
    readonly #pool: sql.ConnectionPool;

    constructor(pool: sql.ConnectionPool) {
        this.#pool = pool;
    }

    async nextId(): Promise<string> {
        const result = await this.#pool.request().query<{ Id: number }>("SELECT NEXT VALUE FOR dbo.OrderNumbers AS Id");
        return toOrderId(result.recordset[0].Id);
    }

    // The order, its lines and the stock change commit together or not at all
    async add(order: Order): Promise<void> {
        const tx = new sql.Transaction(this.#pool);
        await tx.begin(sql.ISOLATION_LEVEL.READ_COMMITTED);
        let rolledBack = false;
        // SQL Server may roll the transaction back itself (deadlock victim, XACT_ABORT)
        tx.on("rollback", () => {
            rolledBack = true;
        });
        try {
            await new sql.Request(tx)
                .input("id", sql.Int, fromOrderId(order.id))
                .input("customerId", sql.NVarChar(64), order.customerId)
                .input("status", sql.VarChar(20), order.status)
                .input("currency", sql.Char(3), order.currency)
                .input("createdAtUtc", sql.DateTime2(3), new Date(order.createdAtUtc))
                .query("INSERT dbo.Orders (Id, CustomerId, Status, Currency, CreatedAtUtc) VALUES (@id, @customerId, @status, @currency, @createdAtUtc)");

            const missing: string[] = [];
            // One request at a time: a transaction is a single connection
            for (const [index, item] of order.items.entries()) {
                const result = await new sql.Request(tx)
                    .input("orderId", sql.Int, fromOrderId(order.id))
                    .input("lineNumber", sql.Int, index + 1)
                    .input("isbn", sql.Char(13), item.isbn)
                    .input("quantity", sql.Int, item.quantity)
                    .input("unitPrice", sql.Decimal(10, 2), toAmount(item.unitPriceMinor))
                    .query(`UPDATE dbo.StockItems SET Quantity = Quantity - @quantity WHERE Isbn = @isbn AND Quantity >= @quantity;
                            INSERT dbo.OrderItems (OrderId, LineNumber, Isbn, Quantity, UnitPrice) VALUES (@orderId, @lineNumber, @isbn, @quantity, @unitPrice);`);
                // rowsAffected has one entry per statement: [UPDATE, INSERT]
                if (result.rowsAffected[0] === 0) {
                    missing.push(item.isbn);
                }
            }
            if (missing.length > 0) {
                throw new OutOfStockError(missing);
            }
            await tx.commit();
        } catch (err) {
            if (!rolledBack) {
                await tx.rollback();
            }
            throw err;
        }
    }

    async findById(id: string): Promise<Order | undefined> {
        const orderId = fromOrderId(id);
        if (orderId === undefined) {
            return undefined;
        }
        // Two result sets in one round trip, like Dapper's QueryMultiple
        const result = await this.#pool.request().input("id", sql.Int, orderId).query(`
            SELECT Id, CustomerId, Status, Currency, CreatedAtUtc FROM dbo.Orders WHERE Id = @id;
            SELECT Isbn, Quantity, CAST(UnitPrice * 100 AS int) AS UnitPriceMinor FROM dbo.OrderItems WHERE OrderId = @id ORDER BY LineNumber;`);
        const [orders, items] = result.recordsets as unknown as [OrderRow[], OrderItemRow[]];
        const row = orders[0];
        if (!row) {
            return undefined;
        }
        return {
            id: toOrderId(row.Id),
            customerId: row.CustomerId,
            status: row.Status,
            currency: row.Currency,
            items: items.map((i) => ({ isbn: i.Isbn, quantity: i.Quantity, unitPriceMinor: i.UnitPriceMinor })),
            createdAtUtc: row.CreatedAtUtc.toISOString(),
        };
    }

    // Rows flow from SQL Server with backpressure: the request pauses while the stream's buffer is full
    exportRows(): Readable {
        const request = new sql.Request(this.#pool);
        const rows = request.toReadableStream({ highWaterMark: 500 });
        request.query(`
            SELECT o.Id, o.CreatedAtUtc, o.CustomerId, i.Isbn, b.Title, i.Quantity,
                   CAST(i.UnitPrice * 100 AS int) AS UnitPriceMinor, o.Currency, o.Status
            FROM dbo.Orders AS o
            JOIN dbo.OrderItems AS i ON i.OrderId = o.Id
            JOIN dbo.Books AS b ON b.Isbn = i.Isbn
            ORDER BY o.Id, i.LineNumber`);
        // A destroyed stream does not stop the query by itself (client gone, gzip failed...)
        rows.on("close", () => {
            if (!rows.readableEnded) {
                request.cancel();
            }
        });
        const toExportRow = new Transform({
            objectMode: true,
            transform(row, encoding, callback) {
                const exportRow: OrderExportRow = {
                    orderId: toOrderId(row.Id),
                    createdAtUtc: (row.CreatedAtUtc as Date).toISOString(),
                    customerId: row.CustomerId,
                    isbn: row.Isbn,
                    title: row.Title,
                    quantity: row.Quantity,
                    unitPriceMinor: row.UnitPriceMinor,
                    currency: row.Currency,
                    status: row.Status,
                };
                callback(null, exportRow);
            },
        });
        // pipeline, not pipe: destroying the returned stream also destroys the SQL stream
        pipeline(rows, toExportRow, () => {});
        return toExportRow;
    }
}
