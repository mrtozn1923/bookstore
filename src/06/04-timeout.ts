import { concat, debounceTime, NEVER, of, switchMap, timeout } from "rxjs";
import { books } from "../../data/books.ts";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeRequest } from "../04/fake-api.ts";

const dune = books[0];

runInVirtualTime("timeout first + with: fall back to the cached stock", () => {
  const cachedStock = { isbn: dune.isbn, stock: 12, cached: true };
  fakeRequest(`GET /books/${dune.isbn}/stock`, 3000, () => ({ isbn: dune.isbn, stock: 11, cached: false }))
    .pipe(timeout({ first: 1000, with: () => of(cachedStock) }))
    .subscribe((s) => log(`stock ${s.stock} (cached: ${s.cached})`));
});

runInVirtualTime("timeout each: a live stream that goes silent", () => {
  // stock updates arrive every 2 s, then the warehouse feed stops after 6 s
  const stock$ = concat(emitAt([2000, 12], [4000, 11], [6000, 10]), NEVER);
  stock$.pipe(timeout({ each: 5000 })).subscribe({
    next: (n) => log(`stock ${n}`),
    error: (err: Error) => log(`${err.name}: ${err.message}`),
  });
});

function searchBooks(term: string) {
  return fakeRequest(`GET /books?q=${term}`, 300, () => books.filter((b) => b.title.toLowerCase().includes(term)).length);
}

runInVirtualTime("timeout outside switchMap: measures the user, not the request", () => {
  const terms$ = concat(emitAt([0, "dun"], [400, "dune"]), NEVER); // the user stops typing, the box stays open
  terms$
    .pipe(
      debounceTime(300),
      switchMap((term) => searchBooks(term)),
      timeout(2000), // wrong place
    )
    .subscribe({ next: (n) => log(`${n} result(s)`), error: (err: Error) => log(`search box dead: ${err.name}`) });
});

runInVirtualTime("timeout inside switchMap: measures each request", () => {
  const terms$ = concat(emitAt([0, "dun"], [400, "dune"]), NEVER);
  terms$
    .pipe(
      debounceTime(300),
      switchMap((term) => searchBooks(term).pipe(timeout(2000))),
    )
    .subscribe({ next: (n) => log(`${n} result(s)`), error: (err: Error) => log(`search box dead: ${err.name}`) });
});
