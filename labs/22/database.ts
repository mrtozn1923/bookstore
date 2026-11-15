import type { ColumnType, Generated } from "kysely";

// Kysely's view of the tables: written by hand here, generated from the database by kysely-codegen in real projects
export interface Database {
    categories: { id: Generated<number>; slug: string; name: string };
    authors: { id: Generated<number>; name: string };
    books: {
        isbn: string;
        title: string;
        author_id: number;
        category_id: number;
        // numeric comes back from pg as a string
        price: ColumnType<string, string | number, string | number>;
        currency: string;
        stock: ColumnType<string, number | undefined, number>;
        added_at: ColumnType<Date, Date | undefined, never>;
    };
}
