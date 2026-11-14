import { Observable } from "rxjs";

const apiUrl = "http://localhost:5180/api";

interface StockInfo {
  isbn: string;
  stock: number;
}

function getStock(isbn: string): Observable<StockInfo> {
  return new Observable<StockInfo>((subscriber) => {
    const controller = new AbortController();
    console.log(`GET /books/${isbn}/stock`);

    fetch(`${apiUrl}/books/${isbn}/stock`, { signal: controller.signal })
      .then((response) => response.json() as Promise<StockInfo>)
      .then((info) => {
        subscriber.next(info);
        subscriber.complete();
      })
      .catch((err) => subscriber.error(err));

    // teardown: runs after unsubscribe or complete
    return () => controller.abort();
  });
}

const duneStock$ = getStock("9780441172719");

// two places subscribe to the same Observable
duneStock$.subscribe((info) => console.log("badge:", info.stock));
duneStock$.subscribe((info) => console.log("cart warning:", info.stock < 3));

// a third subscriber gives up before the response arrives
const subscription = duneStock$.subscribe((info) => console.log("never printed", info));
setTimeout(() => subscription.unsubscribe(), 50);
