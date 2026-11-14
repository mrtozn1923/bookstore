import { defer } from "rxjs";
import { TestScheduler } from "rxjs/testing";
import { beforeEach, expect, it } from "vitest";
import { retryWithBackoff } from "../11/operators.ts";

let testScheduler: TestScheduler;

beforeEach(() => {
  testScheduler = new TestScheduler((actual, expected) => expect(actual).toEqual(expected));
});

const httpError = (status: number) => ({ status });
const isRetryable = (err: unknown) => (err as { status: number }).status >= 500;

it("waits 500, 1000 and 2000 ms, then gives up", () => {
  testScheduler.run(({ cold, expectObservable, expectSubscriptions }) => {
    const request = cold("-#", undefined, httpError(503));
    // random: () => 1 removes the jitter, so the delays are the ceilings
    const result = request.pipe(retryWithBackoff({ count: 3, isRetryable, random: () => 1 }));

    expectObservable(result).toBe("3504ms #", undefined, httpError(503));
    expectSubscriptions(request.subscriptions).toBe([
      "^!",
      "501ms ^!",
      "1502ms ^!",
      "3503ms ^!",
    ]);
  });
});

it("does not retry a 404", () => {
  testScheduler.run(({ cold, expectObservable, expectSubscriptions }) => {
    const request = cold("-#", undefined, httpError(404));
    const result = request.pipe(retryWithBackoff({ count: 3, isRetryable, random: () => 1 }));

    expectObservable(result).toBe("-#", undefined, httpError(404));
    expectSubscriptions(request.subscriptions).toBe("^!");
  });
});

it("succeeds on the third attempt", () => {
  testScheduler.run(({ cold, expectObservable }) => {
    let attempt = 0;
    // every subscription is a new attempt: two 503s, then the answer
    const request = defer(() =>
      ++attempt < 3 ? cold("-#", undefined, httpError(503)) : cold("-(s|)", { s: 12 }),
    );
    const result = request.pipe(retryWithBackoff({ count: 3, isRetryable, random: () => 1 }));

    expectObservable(result).toBe("1503ms (s|)", { s: 12 });
  });
});
