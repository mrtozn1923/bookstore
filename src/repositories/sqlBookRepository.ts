import sql from "mssql";
import type { Book, Currency } from "#domain/book";
import type { BookQuery, BookRepository, BookSort } from "#repositories/bookRepository";

// Never a column name from user input: the API's sort values map to fixed SQL fragments
const ORDER_BY: Record<BookSort, string> = {
    title: "b.Title",
    price: "b.Price, b.Title",
    "-price": "b.Price DESC, b.Title",
};

interface BookRow {
    Isbn: string;
    Title: string;
    Author: string;
    PriceMinor: number;
    Currency: Currency;
}

// Decimal to integer happens in SQL, where decimal arithmetic is exact
const BOOK_COLUMNS = "b.Isbn, b.Title, a.Name AS Author, CAST(b.Price * 100 AS int) AS PriceMinor, b.Currency";
const FROM_BOOKS = "FROM dbo.Books AS b JOIN dbo.Authors AS a ON a.Id = b.AuthorId";

// Hand-written mapping, as with Dapper: one place that knows the column names
function toBook(row: BookRow): Book {
    return { isbn: row.Isbn, title: row.Title, author: row.Author, priceMinor: row.PriceMinor, currency: row.Currency };
}

// LIKE treats % and _ as wildcards even inside a parameter, so escape them
function likePattern(text: string): string {
    return `%${text.replace(/[\\%_[]/g, (ch) => `\\${ch}`)}%`;
}

export class SqlBookRepository implements BookRepository {
    readonly #pool: sql.ConnectionPool;

    constructor(pool: sql.ConnectionPool) {
        this.#pool = pool;
    }

    async list({ search, author, sort, offset, limit }: BookQuery): Promise<{ items: Book[]; totalCount: number }> {
        const result = await this.#pool
            .request()
            .input("search", sql.NVarChar(102), search ? likePattern(search) : null)
            .input("author", sql.NVarChar(200), author ?? null)
            .input("offset", sql.Int, offset)
            .input("limit", sql.Int, limit)
            .query<BookRow & { TotalCount: number }>(`SELECT ${BOOK_COLUMNS}, COUNT(*) OVER () AS TotalCount ${FROM_BOOKS}
                WHERE (@search IS NULL OR b.Title LIKE @search ESCAPE '\\')
                  AND (@author IS NULL OR a.Name = @author)
                ORDER BY ${ORDER_BY[sort]}
                OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY`);
        const rows = result.recordset;
        // COUNT(*) OVER () repeats the total on every row; an empty page needs a separate count
        const totalCount = rows.length > 0 ? rows[0].TotalCount : offset > 0 ? await this.#count(search, author) : 0;
        return { items: rows.map(toBook), totalCount };
    }

    async #count(search: string | undefined, author: string | undefined): Promise<number> {
        const result = await this.#pool
            .request()
            .input("search", sql.NVarChar(102), search ? likePattern(search) : null)
            .input("author", sql.NVarChar(200), author ?? null)
            .query<{ TotalCount: number }>(`SELECT COUNT(*) AS TotalCount ${FROM_BOOKS}
                WHERE (@search IS NULL OR b.Title LIKE @search ESCAPE '\\') AND (@author IS NULL OR a.Name = @author)`);
        return result.recordset[0].TotalCount;
    }

    async findByIsbn(isbn: string): Promise<Book | undefined> {
        // Tagged template: every ${} becomes a parameter (@param1), never part of the SQL text
        const result = await this.#pool.request().query<BookRow>`
            SELECT b.Isbn, b.Title, a.Name AS Author, CAST(b.Price * 100 AS int) AS PriceMinor, b.Currency
            FROM dbo.Books AS b JOIN dbo.Authors AS a ON a.Id = b.AuthorId
            WHERE b.Isbn = ${isbn}`;
        return result.recordset.map(toBook)[0];
    }

    async findManyByIsbn(isbns: readonly string[]): Promise<Map<string, Book>> {
        // One parameter for the whole list: a JSON array that SQL Server opens with OPENJSON
        const result = await this.#pool
            .request()
            .input("isbns", sql.NVarChar(sql.MAX), JSON.stringify(isbns))
            .query<BookRow>(`SELECT ${BOOK_COLUMNS} ${FROM_BOOKS} WHERE b.Isbn IN (SELECT value FROM OPENJSON(@isbns))`);
        return new Map(result.recordset.map((row) => [row.Isbn, toBook(row)]));
    }
}
