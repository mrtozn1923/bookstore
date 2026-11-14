import { Observable } from "rxjs";

// 1) Function: pull, a single value
function readStock(): number {
  return 12;
}
console.log("function:", readStock());

// 2) Iterator: pull, many values; the consumer calls next()
function* stockHistory() {
  yield 12;
  yield 11;
  yield 10;
}
const it = stockHistory();
console.log("iterator:", it.next().value, it.next().value);

// 3) Promise: push, a single value; starts working immediately
const stockPromise = new Promise<number>((resolve) => {
  console.log("promise executor runs now");
  setTimeout(() => resolve(12), 10);
});

// 4) Observable: push, many values; nothing runs until subscribe
const stock$ = new Observable<number>((subscriber) => {
  console.log("observable producer runs now");
  subscriber.next(12);
  setTimeout(() => {
    subscriber.next(11);
    subscriber.complete();
  }, 20);
});

console.log("--- subscribing ---");
stockPromise.then((v) => console.log("promise:", v));
stock$.subscribe({
  next: (v) => console.log("observable:", v),
  complete: () => console.log("observable: complete"),
});
