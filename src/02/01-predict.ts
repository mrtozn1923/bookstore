import { defer, from, retry } from "rxjs";

const apiUrl = "http://localhost:5180/api";

interface PriceInfo {
  isbn: string;
  price: number;
  currency: string;
}

// fail = N: the mock API answers 503 to the first N requests for that book
async function fetchPrice(isbn: string, fail = 0): Promise<PriceInfo> {
  const response = await fetch(`${apiUrl}/books/${isbn}/price?fail=${fail}`);
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  return (await response.json()) as PriceInfo;
}

const emmaPrice$ = from(fetchPrice("9780141439587")); // nobody subscribes to this one
const dunePrice$ = from(fetchPrice("9780441172719", 2));
const hobbitPrice$ = defer(() => fetchPrice("9780547928227", 2));

dunePrice$.pipe(retry(2)).subscribe({
  next: (p) => console.log("Dune:", p.price, p.currency),
  error: (err: Error) => console.log("Dune failed:", err.message),
});
hobbitPrice$.pipe(retry(2)).subscribe({
  next: (p) => console.log("The Hobbit:", p.price, p.currency),
  error: (err: Error) => console.log("The Hobbit failed:", err.message),
});
