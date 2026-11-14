import { defer, map, Subject, switchMap, throwError, timer } from "rxjs";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { retryWithBackoff, settledTerm } from "../11/operators.ts";

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

it("emits a term 300 ms after the last key", () => {
  const input = new Subject<string>();
  const terms: string[] = [];
  input.pipe(settledTerm(300)).subscribe((term) => terms.push(term));

  for (const term of ["d", "du", "dun", "dune"]) {
    input.next(term);
    vi.advanceTimersByTime(100);
  }
  // 100 ms after "dune": nothing yet
  expect(terms).toEqual([]);
  vi.advanceTimersByTime(200);
  expect(terms).toEqual(["dune"]);
});

it("cancels the request for a term the user has moved on from", () => {
  const input = new Subject<string>();
  const requests: string[] = [];
  const results: string[][] = [];
  input
    .pipe(
      settledTerm(300),
      switchMap((term) => {
        requests.push(term);
        return timer(500).pipe(map(() => [term]));
      }),
    )
    .subscribe((titles) => results.push(titles));

  input.next("em");
  vi.advanceTimersByTime(400);
  input.next("emma");
  vi.advanceTimersByTime(1000);

  expect(requests).toEqual(["em", "emma"]);
  expect(results).toEqual([["emma"]]);
});

it("waits 500, 1000 and 2000 ms, then gives up", () => {
  const attempts: number[] = [];
  let failure: unknown;
  defer(() => {
    attempts.push(Date.now());
    return throwError(() => ({ status: 503 }));
  })
    .pipe(retryWithBackoff({ count: 3, random: () => 1 }))
    .subscribe({ error: (err: unknown) => (failure = err) });

  vi.runAllTimers();

  const start = attempts[0];
  expect(attempts.map((t) => t - start)).toEqual([0, 500, 1500, 3500]);
  expect(failure).toEqual({ status: 503 });
});
