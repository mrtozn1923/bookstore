import type { Book } from "./books";

export async function fetchBooks(url: string): Promise<Book[]> {
    const response = await fetch(url, { headers: { Accept: "application/json" } });
    if (!response.ok) {
        throw new Error(`GET ${url} failed with status ${response.status}`);
    }
    return await response.json() as Book[];
}
