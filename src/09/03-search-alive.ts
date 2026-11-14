import { catchError, concat, map, NEVER, of, switchMap } from "rxjs";
import { books } from "../../data/books.ts";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeHttp } from "./fake-http.ts";

type SearchResult = { term: string; titles: string[] } | { term: string; error: string };

function searchBooks(term: string) {
  const status = term === "dun" ? 503 : 200;
  return fakeHttp(`GET /books?q=${term}`, 200, status, () =>
    books.filter((b) => b.title.toLowerCase().includes(term)).map((b) => b.title),
  );
}

runInVirtualTime("search box, catchError inside switchMap", () => {
  const terms$ = concat(emitAt([0, "du"], [1000, "dun"], [2000, "dune"]), NEVER);
  terms$
    .pipe(
      switchMap((term) =>
        searchBooks(term).pipe(
          map((titles): SearchResult => ({ term, titles })),
          // the error ends only this inner request; the outer stream keeps listening
          catchError((err: Error) => of<SearchResult>({ term, error: err.message })),
        ),
      ),
    )
    .subscribe((r) =>
      log("error" in r ? `screen: search for "${r.term}" failed (${r.error}), try again` : `screen: [${r.titles.join(", ")}]`),
    );
});
