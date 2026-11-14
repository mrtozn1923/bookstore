import "./styles.css";
import { fetchBooks } from "./api";
import { sampleBooks, type Book } from "./books";
import { mountBookFinder } from "./finder";

async function start(host: HTMLElement): Promise<void> {
    const source = host.dataset.source;
    const books: readonly Book[] = source ? await fetchBooks(source) : sampleBooks;
    mountBookFinder(host, books);
}

const host = document.querySelector<HTMLElement>("#book-finder");
if (host) {
    void start(host);
}
