import { catchError, concat, NEVER, of, switchMap } from "rxjs";
import { books } from "../../data/books.ts";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeHttp } from "./fake-http.ts";

// the search API fails once, for "dun"
function searchBooks(term: string) {
  const status = term === "dun" ? 503 : 200;
  return fakeHttp(`GET /books?q=${term}`, 200, status, () =>
    books.filter((b) => b.title.toLowerCase().includes(term)).map((b) => b.title),
  );
}

runInVirtualTime("search box, catchError at the end", () => {
  const terms$ = concat(emitAt([0, "du"], [1000, "dun"], [2000, "dune"]), NEVER); // the box stays open
  terms$
    .pipe(
      switchMap((term) => searchBooks(term)),
      catchError(() => of<string[]>([])),
    )
    .subscribe({
      next: (titles) => log(`screen: [${titles.join(", ")}]`),
      complete: () => log("search stream completed"),
    });
});
