import { DummyDriver, Kysely, MssqlAdapter, MssqlIntrospector, MssqlQueryCompiler, PostgresDialect } from "kysely";
import pg from "pg";
import type { Database } from "./database.ts";

// The query as a function of the Kysely instance; every table and column name is checked by tsc
function booksByCategory(db: Kysely<Database>, slug: string, limit: number) {
    return db
        .selectFrom("books as b")
        .innerJoin("authors as a", "a.id", "b.author_id")
        .innerJoin("categories as c", "c.id", "b.category_id")
        .select(["b.isbn", "b.title", "a.name as author", "b.price", "b.currency"])
        .where("c.slug", "=", slug)
        .orderBy("b.title")
        .limit(limit);
}

const db = new Kysely<Database>({ dialect: new PostgresDialect({ pool: new pg.Pool({ connectionString: process.env.DATABASE_URL }) }) });
const query = booksByCategory(db, "classics", 10);
const compiled = query.compile();
console.log(compiled.sql);
console.log(compiled.parameters);
const rows = await query.execute();
console.log(rows.map((r) => `${r.title} | ${r.author} | ${r.price}`).join("\n"));
await db.destroy();

// The same function compiled for SQL Server without a connection: what would tedious receive?
const sqlServer = new Kysely<Database>({
    dialect: {
        createAdapter: () => new MssqlAdapter(),
        createDriver: () => new DummyDriver(),
        createIntrospector: (db) => new MssqlIntrospector(db),
        createQueryCompiler: () => new MssqlQueryCompiler(),
    },
});
console.log(booksByCategory(sqlServer, "classics", 10).compile().sql);

// SQL Server has no LIMIT: there the query needs top() (or offset/fetch)
const tsql = sqlServer
    .selectFrom("books as b")
    .innerJoin("categories as c", "c.id", "b.category_id")
    .top(10)
    .select(["b.isbn", "b.title"])
    .where("c.slug", "=", "classics")
    .orderBy("b.title");
console.log(tsql.compile().sql);
