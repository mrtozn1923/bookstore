import { log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { isRetryable, seededRandom } from "../09/backoff.ts";
import { flakyHttp } from "../09/fake-http.ts";
import { retryWithBackoff } from "./operators.ts";

runInVirtualTime("503, 503, then 200", () => {
  flakyHttp("GET /books/9780441172719/stock", 200, [503, 503, 200], () => 12)
    .pipe(
      retryWithBackoff({
        count: 4,
        isRetryable,
        random: seededRandom(42),
        onRetry: (err, retryCount, delayMs) => log(`retry ${retryCount} in ${delayMs} ms (${String(err)})`),
      }),
    )
    .subscribe({
      next: (stock) => log(`stock: ${stock}`),
      error: (err: Error) => log(`stock failed: ${err.message}`),
    });
});

runInVirtualTime("404 is not retried", () => {
  flakyHttp("GET /books/9780000000000", 200, [404], () => "book")
    .pipe(retryWithBackoff({ count: 4, isRetryable }))
    .subscribe({
      next: (book) => log(`book: ${book}`),
      error: (err: Error) => log(`book failed: ${err.message}`),
    });
});
