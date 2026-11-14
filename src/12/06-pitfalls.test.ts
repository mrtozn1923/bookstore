import { defer, delay, firstValueFrom, of } from "rxjs";
import { TestScheduler } from "rxjs/testing";
import { beforeEach, expect, it } from "vitest";

let testScheduler: TestScheduler;

beforeEach(() => {
  testScheduler = new TestScheduler((actual, expected) => expect(actual).toEqual(expected));
});

// it.fails: this test is expected to fail, see the post for the message
it.fails("a Promise does not run on virtual time", () => {
  testScheduler.run(({ expectObservable }) => {
    const price = defer(() => Promise.resolve(450));
    expectObservable(price).toBe("(p|)", { p: 450 });
  });
});

it("delay outside run() waits in real time", async () => {
  const term = await firstValueFrom(of("dune").pipe(delay(1000)));
  expect(term).toBe("dune");
});

it("delay inside run() uses virtual time", () => {
  testScheduler.run(({ expectObservable }) => {
    expectObservable(of("dune").pipe(delay(1000))).toBe("1000ms (a|)", { a: "dune" });
  });
});
