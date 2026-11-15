import pg from "pg";

const pool = new pg.Pool({ connectionString: process.env.DATABASE_URL });

// Total value of the classics on the shelf: price * stock, added up
const { rows } = await pool.query("SELECT b.price, b.stock FROM books b JOIN categories c ON c.id = b.category_id WHERE c.slug = $1", ["classics"]);
const total = rows.reduce((sum, row) => sum + row.price * row.stock, 0);
const prices = rows.reduce((sum, row) => sum + row.price, 0);
console.log("stock value:", total);
console.log("sum of prices:", prices);
await pool.end();
