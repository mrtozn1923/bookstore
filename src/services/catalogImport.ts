import { readFile } from "node:fs/promises";
import { isValidIsbn13 } from "@bookstore/contracts";
import type { Book, Currency } from "#domain/book";

export interface CatalogImportResult {
    books: Book[];
    errors: string[];
}

const HEADER = ["isbn", "title", "author", "price", "currency"];
const CURRENCIES: readonly string[] = ["TRY", "EUR"] satisfies Currency[];

// Excel's "CSV UTF-8" in a Turkish locale: optional BOM, ';' separator, ',' decimal mark
export function parseCatalogCsv(text: string): CatalogImportResult {
    const lines = text.replace(/^﻿/, "").split(/\r?\n/);
    const header = lines[0].split(";").map((c) => c.trim().toLowerCase());
    if (header.join(";") !== HEADER.join(";")) {
        throw new Error(`unexpected header: ${header.join(";")}`);
    }

    const books: Book[] = [];
    const errors: string[] = [];
    lines.slice(1).forEach((line, index) => {
        const lineNo = index + 2;
        if (line.trim() === "") {
            return;
        }
        const fields = line.split(";").map((f) => f.trim());
        if (fields.length !== HEADER.length) {
            errors.push(`line ${lineNo}: expected ${HEADER.length} fields, got ${fields.length}`);
            return;
        }
        const [isbn, title, author, priceText, currency] = fields;
        const price = Number(priceText.replace(",", "."));
        if (!isValidIsbn13(isbn)) {
            errors.push(`line ${lineNo}: invalid ISBN-13 ${isbn}`);
        } else if (!Number.isFinite(price) || price <= 0) {
            errors.push(`line ${lineNo}: invalid price ${priceText}`);
        } else if (!CURRENCIES.includes(currency)) {
            errors.push(`line ${lineNo}: unknown currency ${currency}`);
        } else {
            books.push({ isbn, title, author, price, currency: currency as Currency });
        }
    });
    return { books, errors };
}

// Small files only: the whole file is read into memory (large files: streams, next post)
export async function readCatalogCsv(csvPath: string): Promise<CatalogImportResult> {
    return parseCatalogCsv(await readFile(csvPath, "utf8"));
}
