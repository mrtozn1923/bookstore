// Usage: node --env-file=.env scripts/load-catalog.ts <books.json> [initialStock]
import { readFile } from "node:fs/promises";
import sql from "mssql";
import type { Book } from "#domain/book";
import { toAmount } from "#domain/money";

const [jsonPath, stockText = "10"] = process.argv.slice(2);
const connectionString = process.env.DB_CONNECTION_STRING;
if (!jsonPath || !connectionString) {
    console.error("usage: DB_CONNECTION_STRING=... node scripts/load-catalog.ts <books.json> [initialStock]");
    process.exit(2);
}

const books: Book[] = JSON.parse(await readFile(jsonPath, "utf8"));
const pool = await new sql.ConnectionPool(connectionString).connect();
const tx = new sql.Transaction(pool);
await tx.begin();
let inserted = 0;
try {
    // One stored procedure call per book, all inside one transaction: the catalog loads completely or not at all
    for (const book of books) {
        const result = await new sql.Request(tx)
            .input("Isbn", sql.Char(13), book.isbn)
            .input("Title", sql.NVarChar(200), book.title)
            .input("Author", sql.NVarChar(200), book.author)
            .input("Price", sql.Decimal(10, 2), toAmount(book.priceMinor))
            .input("Currency", sql.Char(3), book.currency)
            .input("InitialStock", sql.Int, Number(stockText))
            .output("Inserted", sql.Bit)
            .execute("dbo.UpsertBook");
        inserted += result.output.Inserted ? 1 : 0;
    }
    await tx.commit();
} catch (err) {
    await tx.rollback();
    throw err;
} finally {
    await pool.close();
}
console.log(`loaded ${books.length} books from ${jsonPath}: ${inserted} inserted, ${books.length - inserted} updated`);
