import { retry } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { retryDelay, seededRandom } from "./backoff.ts";
import { flakyHttp } from "./fake-http.ts";

const report = (label: string) => ({
  next: (v: unknown) => log(`${label}: ${v}`),
  error: (err: Error) => log(`${label} failed: ${err.message}`),
});

runInVirtualTime("503, 503, 503, then 200", () => {
  flakyHttp("GET /books/9780441172719/stock", 200, [503, 503, 503, 200], () => 12)
    .pipe(retry({ count: 4, delay: retryDelay(seededRandom(42)) }))
    .subscribe(report("stock"));
});

runInVirtualTime("404 is not retried", () => {
  flakyHttp("GET /books/9780000000000", 200, [404], () => "book")
    .pipe(retry({ count: 4, delay: retryDelay(seededRandom(42)) }))
    .subscribe(report("book"));
});

runInVirtualTime("503 every time: give up after 4 retries", () => {
  flakyHttp("GET /categories", 200, [503], () => [])
    .pipe(retry({ count: 4, delay: retryDelay(seededRandom(42)) }))
    .subscribe(report("categories"));
});
