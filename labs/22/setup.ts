// Creates the lab schema in PostgreSQL: node --env-file=.env setup.ts
import { readFile } from "node:fs/promises";
import pg from "pg";

const client = new pg.Client({ connectionString: process.env.DATABASE_URL });
await client.connect();
await client.query(await readFile(new URL("./sql/schema.sql", import.meta.url), "utf8"));
const { rows } = await client.query("SELECT count(*) AS books FROM books");
console.log(`schema ready, ${rows[0].books} books`);
await client.end();
