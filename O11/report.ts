import type { Book } from "./book.ts";
import { formatBook } from "./book.ts";

export type { Book };

export function report(books: readonly Book[]): string {
  return books.map(formatBook).join("\n");
}
