import { switchMap, tap } from "rxjs";
import { TestScheduler } from "rxjs/testing";
import { beforeEach, expect, it } from "vitest";
import { settledTerm } from "../11/operators.ts";

let testScheduler: TestScheduler;

beforeEach(() => {
  testScheduler = new TestScheduler((actual, expected) => expect(actual).toEqual(expected));
});

it("cancels the request for a term the user has moved on from", () => {
  testScheduler.run(({ hot, cold, expectObservable, expectSubscriptions, flush }) => {
    const input = hot("a 399ms b", { a: "em", b: "emma" });
    // fake API answers: each request takes 500 ms, then completes
    const responses = {
      em: cold("500ms r|", { r: ["Emma"] }),
      emma: cold("500ms r|", { r: ["Emma"] }),
    };
    const requests: string[] = [];

    const results = input.pipe(
      settledTerm(300),
      tap((term) => requests.push(term)),
      switchMap((term) => responses[term as keyof typeof responses]),
    );

    expectObservable(results).toBe("1200ms r", { r: ["Emma"] });
    // "em" starts at 300 ms and is dropped at 700 ms, when "emma" settles
    expectSubscriptions(responses.em.subscriptions).toBe("300ms ^ 399ms !");
    expectSubscriptions(responses.emma.subscriptions).toBe("700ms ^ 500ms !");

    // flush runs the virtual clock now, so side effects can be checked inside run()
    flush();
    expect(requests).toEqual(["em", "emma"]);
  });
});
