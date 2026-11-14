import { map, Observable, take, tap, timer, type MonoTypeOperatorFunction } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";

// a hand-written operator: pass a stock value on only when it differs from the previous one
function stockChanges(): MonoTypeOperatorFunction<number> {
  return (source) =>
    new Observable<number>((subscriber) => {
      let previous: number | undefined;
      source.subscribe({
        next: (stock) => {
          if (stock !== previous) subscriber.next(stock);
          previous = stock;
        },
        error: (err: unknown) => subscriber.error(err),
        complete: () => subscriber.complete(),
      });
    });
}

// Dune's stock as the API reports it on each poll
const answers = [12, 12, 11, 11, 11, 10, 10, 9];

runInVirtualTime("first two stock changes of Dune", () => {
  timer(0, 1000)
    .pipe(
      tap((i) => log(`-> GET /books/9780441172719/stock (poll ${i + 1})`)),
      map((i) => answers[i % answers.length]),
      stockChanges(),
      take(2),
    )
    .subscribe({
      next: (stock) => log(`screen: ${stock} in stock`),
      complete: () => log("done"),
    });
}, 5000);
