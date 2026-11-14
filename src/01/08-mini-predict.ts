import { MiniObservable } from "./mini-observable.ts";

const duneStock$ = new MiniObservable<number>((subscriber) => {
  console.log("producer started");
  subscriber.next(12);
  subscriber.next(11);
  subscriber.complete();
  subscriber.next(10);
  return () => console.log("teardown");
});

console.log("before subscribe");
duneStock$.subscribe({ next: (stock) => console.log("A got", stock) });
duneStock$.subscribe({ next: (stock) => console.log("B got", stock) });
console.log("after subscribe");

const broken$ = new MiniObservable<number>(() => {
  throw new Error("stock service unavailable");
});
broken$.subscribe({ error: (err) => console.log("error:", (err as Error).message) });
