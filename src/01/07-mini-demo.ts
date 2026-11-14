import { MiniObservable, map, filter } from "./mini-observable.ts";

const stock$ = new MiniObservable<number>((subscriber) => {
  console.log("producer started");
  let stock = 5;
  const id = setInterval(() => {
    stock -= 1;
    subscriber.next(stock);
    if (stock === 0) subscriber.complete();
  }, 100);
  return () => {
    clearInterval(id);
    console.log("teardown");
  };
});

const lowStock$ = stock$.pipe(
  filter((s: number) => s < 3),
  map((s: number) => `only ${s} left`),
);

console.log("nothing has run yet");
lowStock$.subscribe({
  next: (msg) => console.log(msg),
  complete: () => console.log("complete"),
});
