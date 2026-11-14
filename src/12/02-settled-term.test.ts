import { TestScheduler } from "rxjs/testing";
import { beforeEach, expect, it } from "vitest";
import { settledTerm } from "../11/operators.ts";

let testScheduler: TestScheduler;

beforeEach(() => {
  // the scheduler compares the recorded frames with deep equality; Vitest reports the difference
  testScheduler = new TestScheduler((actual, expected) => expect(actual).toEqual(expected));
});

it("emits a term 300 ms after the last key", () => {
  testScheduler.run(({ hot, expectObservable }) => {
    // "d", "du", "dun", "dune" 100 ms apart: a value takes one frame, so "a 99ms b" is 100 ms
    const input = hot("a 99ms b 99ms c 99ms d", { a: "d", b: "du", c: "dun", d: "dune" });
    expectObservable(input.pipe(settledTerm(300))).toBe("600ms d", { d: "dune" });
  });
});

it("trims the term and skips a repeat", () => {
  testScheduler.run(({ hot, expectObservable }) => {
    const input = hot("a 399ms b 399ms c", { a: "dune", b: "dune ", c: " emma" });
    expectObservable(input.pipe(settledTerm(300))).toBe("300ms a 799ms c", { a: "dune", c: "emma" });
  });
});
