import { animationFrameScheduler, auditTime, fromEvent, map, tap } from "rxjs";
import { books } from "../data/books.ts";

const list = document.querySelector<HTMLUListElement>("#books")!;
const progress = document.querySelector<HTMLProgressElement>("#progress")!;
const toTop = document.querySelector<HTMLButtonElement>("#to-top")!;
const scrollStats = document.querySelector<HTMLParagraphElement>("#scroll-stats")!;

// a long catalog page: the six sample books, 25 times
for (let i = 0; i < 25; i++) {
  for (const book of books) list.insertAdjacentHTML("beforeend", `<li>${book.title} - ${book.price} TRY</li>`);
}

let scrollEvents = 0;
let scrollUpdates = 0;

fromEvent(window, "scroll", { passive: true })
  .pipe(
    tap(() => scrollEvents++),
    auditTime(0, animationFrameScheduler), // at most one update per frame, with the latest position
    map(() => window.scrollY),
  )
  .subscribe((y) => {
    scrollUpdates++;
    progress.value = y / (document.documentElement.scrollHeight - window.innerHeight);
    toTop.hidden = y < 600;
    scrollStats.textContent = `scroll events: ${scrollEvents}, screen updates: ${scrollUpdates}`;
  });

fromEvent(toTop, "click").subscribe(() => window.scrollTo({ top: 0 }));

// a source that is not tied to frames: stock messages, e.g. from a WebSocket, many in one task
const stockFeed = new EventTarget();
const stock = document.querySelector<HTMLParagraphElement>("#stock")!;
const stockStats = document.querySelector<HTMLParagraphElement>("#stock-stats")!;
let stockMessages = 0;
let stockUpdates = 0;

fromEvent(stockFeed, "stock")
  .pipe(
    tap(() => stockMessages++),
    map((event) => (event as CustomEvent<number>).detail),
    auditTime(0, animationFrameScheduler),
  )
  .subscribe((count) => {
    stockUpdates++;
    stock.textContent = `Dune stock: ${count}`;
    stockStats.textContent = `stock messages: ${stockMessages}, screen updates: ${stockUpdates}`;
  });

fromEvent(document.querySelector("#simulate")!, "click").subscribe(() => {
  for (let i = 1; i <= 200; i++) stockFeed.dispatchEvent(new CustomEvent("stock", { detail: 212 - i }));
});
