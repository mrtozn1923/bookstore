import { concatMap, exhaustMap, mergeMap, switchMap, type OperatorFunction } from "rxjs";
import { books } from "../../data/books.ts";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeRequest } from "./fake-api.ts";

type SearchResult = { term: string; titles: string[] };

// a short term matches more books, so the API answers it more slowly
const latency: Record<string, number> = { du: 600, dun: 200, dune: 100 };

function searchBooks(term: string) {
  return fakeRequest(`GET /books?q=${term}`, latency[term], (): SearchResult => ({
    term,
    titles: books.filter((b) => b.title.toLowerCase().includes(term)).map((b) => b.title),
  }));
}

function runSearch(name: string, flatten: OperatorFunction<string, SearchResult>) {
  runInVirtualTime(name, () => {
    // the user types "du", "dun", "dune" 150 ms apart
    const terms$ = emitAt([0, "du"], [150, "dun"], [300, "dune"]);
    terms$.pipe(flatten).subscribe((r) => log(`screen: "${r.term}" -> ${r.titles.join(", ")}`));
  });
}

runSearch("mergeMap", mergeMap((term) => searchBooks(term)));
runSearch("concatMap", concatMap((term) => searchBooks(term)));
runSearch("switchMap", switchMap((term) => searchBooks(term)));
runSearch("exhaustMap", exhaustMap((term) => searchBooks(term)));
