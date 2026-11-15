import pg from "pg";

// One pool per process, like the mssql pool of post 13
const pool = new pg.Pool({ connectionString: process.env.DATABASE_URL, max: 10 });

// $1, $2: parameters travel separately from the SQL text
const booksByCategory = `
    SELECT b.isbn, b.title, a.name AS author, b.price, b.currency, b.stock, b.added_at
    FROM books b
    JOIN authors a ON a.id = b.author_id
    JOIN categories c ON c.id = b.category_id
    WHERE c.slug = $1
    ORDER BY b.title
    LIMIT $2`;
const { rows } = await pool.query(booksByCategory, ["classics", 10]);
console.log(rows.map((r) => `${r.title} | ${r.author} | ${r.price} ${r.currency}`).join("\n"));
// numeric and bigint arrive as strings, timestamptz as Date
const [first] = rows;
console.log({ price: typeof first.price, stock: typeof first.stock, addedAt: first.added_at instanceof Date });

// A transaction needs one client from the pool for every statement in it
async function reserveStock(isbn: string, quantity: number): Promise<void> {
    const client = await pool.connect();
    try {
        await client.query("BEGIN");
        const result = await client.query("UPDATE books SET stock = stock - $2 WHERE isbn = $1 AND stock >= $2", [isbn, quantity]);
        if (result.rowCount !== 1) {
            throw new Error(`not enough stock for ${isbn}`);
        }
        await client.query("COMMIT");
    } catch (err) {
        await client.query("ROLLBACK");
        throw err;
    } finally {
        client.release();
    }
}
await reserveStock("9780141439587", 2);
await reserveStock("9780141439471", 1).catch((err: Error) => console.log(err.message));
await pool.end();
