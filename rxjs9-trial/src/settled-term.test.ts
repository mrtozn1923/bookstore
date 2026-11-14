import { rxTest } from "@rxjs/test";
import { expect, it } from "vitest";
import { settledTerm } from "./settled-term.ts";

it("emits a term 300 ms after the last key", async () => {
  await rxTest(({ hot, expectObservable }) => {
    const input = hot("a 99ms b 99ms c 99ms d", { a: "d", b: "du", c: "dun", d: "dune" });
    // rxTest refuses a stream that is still open at the end: unsubscribe at 1000 ms
    expectObservable(settledTerm(input, 300), "^ 999ms !").toBe("600ms d", { d: "dune" });
  });
});

it("trims the term and skips a repeat", async () => {
  await rxTest(({ hot, expectObservable }) => {
    const input = hot("a 399ms b 399ms c", { a: "dune", b: "dune ", c: " emma" });
    expectObservable(settledTerm(input, 300), "^ 1999ms !").toBe("300ms a 799ms c", {
      a: "dune",
      c: "emma",
    });
  });
});
