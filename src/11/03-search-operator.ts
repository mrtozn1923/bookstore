import { concat, map, NEVER, pipe, switchMap, timer, type Observable, type OperatorFunction } from "rxjs";
import { books } from "../../data/books.ts";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { debugTap, settledTerm } from "./operators.ts";

// a fake GET /books?q=…: answers after 500 ms
function findBooks(term: string): Observable<string[]> {
  return timer(500).pipe(
    map(() => books.filter((b) => b.title.toLowerCase().includes(term)).map((b) => b.title)),
  );
}

// the whole search block of the earlier posts, now one named operator
function searchBooks(): OperatorFunction<string, string[]> {
  return pipe(
    settledTerm(300),
    switchMap((term) => findBooks(term).pipe(debugTap(`GET /books?q=${term}`, log))),
  );
}

runInVirtualTime("search box", () => {
  // what the user types; NEVER keeps the input open like a real text box
  const input$ = concat(
    emitAt([0, "d"], [100, "du"], [200, "dun"], [300, "dune"], [1200, "dune "], [1600, "em"], [2000, "emma"]),
    NEVER,
  );
  input$.pipe(searchBooks()).subscribe((titles) => log(`screen: ${titles.join(", ")}`));
});
