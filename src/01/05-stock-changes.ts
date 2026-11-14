import { Observable } from "rxjs";

// A fake producer: Dune's stock drops by one every 100 ms (as if orders came in)
function stockChanges(title: string, start: number): Observable<number> {
  return new Observable<number>((subscriber) => {
    let stock = start;
    console.log(`[${title}] timer started`);
    const id = setInterval(() => {
      stock -= 1;
      subscriber.next(stock);
      if (stock === 0) subscriber.complete();
    }, 100);

    return () => {
      clearInterval(id);
      console.log(`[${title}] timer cleared`);
    };
  });
}

// 1) the consumer stops early: teardown runs on unsubscribe
const sub = stockChanges("Dune", 12).subscribe((s) => console.log("Dune stock:", s));
setTimeout(() => sub.unsubscribe(), 350);

// 2) the producer completes: teardown runs without any unsubscribe call
setTimeout(() => {
  stockChanges("Middlemarch", 3).subscribe({
    next: (s) => console.log("Middlemarch stock:", s),
    complete: () => console.log("Middlemarch: sold out"),
  });
}, 500);
