import { catchError, defer, Observable, of, shareReplay, timer } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeRequest } from "../04/fake-api.ts";

// the first call fails with 503, the server is fine afterwards
function categoriesApi(): Observable<string[]> {
  let calls = 0;
  return defer(() =>
    ++calls === 1
      ? new Observable<string[]>((subscriber) => {
          log("-> GET /categories");
          const pending = timer(200).subscribe(() => {
            log("<- GET /categories 503");
            subscriber.error(new Error("503 Service Unavailable"));
          });
          return () => pending.unsubscribe();
        })
      : fakeRequest("GET /categories", 200, () => ["science-fiction", "classics", "fantasy"]),
  );
}

function render(categories$: Observable<string[]>, label: string) {
  categories$.subscribe({
    next: (c) => log(`${label}: [${c.join(", ")}]`),
    error: (err: Error) => log(`${label}: error ${err.message}`),
  });
}

runInVirtualTime("catchError before shareReplay: the fallback is cached", () => {
  const categories$ = categoriesApi().pipe(
    catchError(() => of([])),
    shareReplay(1),
  );
  render(categories$, "menu");
  timer(5000).subscribe(() => render(categories$, "menu after 5 s"));
});

runInVirtualTime("shareReplay alone: the error is not cached", () => {
  const categories$ = categoriesApi().pipe(shareReplay(1));
  render(categories$.pipe(catchError(() => of([]))), "menu");
  timer(5000).subscribe(() => render(categories$.pipe(catchError(() => of([]))), "menu after 5 s"));
});
