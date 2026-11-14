import { of } from "rxjs";
import { expect, it } from "vitest";
import { settledTerm } from "../11/operators.ts";

it("searches 300 ms after the last key", () => {
  const terms: string[] = [];
  of("d", "du", "dun", "dune").pipe(settledTerm(300)).subscribe((term) => terms.push(term));
  expect(terms).toEqual(["dune"]);
});
