import { debounce } from "lodash-es";
import type { Book } from "./books";
import { formatPrice } from "./format";
import { searchBooks } from "./search";

export function mountBookFinder(host: HTMLElement, books: readonly Book[]): void {
    const input = document.createElement("input");
    input.type = "search";
    input.className = "finder-input";
    input.placeholder = "Search by title or author";
    input.setAttribute("aria-label", "Search books");

    const list = document.createElement("ul");
    list.className = "finder-results";

    const render = (items: Book[]): void => {
        list.replaceChildren(...items.map(book => {
            const item = document.createElement("li");
            item.textContent = `${book.title} · ${book.author} · ${formatPrice(book.price)}`;
            return item;
        }));
    };

    input.addEventListener("input", debounce(() => render(searchBooks(books, input.value)), 200));
    host.replaceChildren(input, list);
    render([...books]);
}
