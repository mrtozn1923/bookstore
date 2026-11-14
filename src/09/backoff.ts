// Exponential backoff with jitter: 500, 1000, 2000… ms (capped), each randomised between 50% and 100%.
import { throwError, timer, type Observable } from "rxjs";
import { HttpError } from "./fake-http.ts";

export function backoffDelay(retryCount: number, random: () => number = Math.random, baseMs = 500, maxMs = 8000): number {
  const ceiling = Math.min(maxMs, baseMs * 2 ** (retryCount - 1));
  return Math.round(ceiling / 2 + (random() * ceiling) / 2);
}

// network errors (status 0) and 5xx may succeed on the next attempt; 4xx will not
export function isRetryable(err: unknown): boolean {
  return err instanceof HttpError && (err.status === 0 || err.status >= 500);
}

// for retry({ delay }): wait for a retryable error, rethrow anything else at once
export function retryDelay(random?: () => number) {
  return (err: unknown, retryCount: number): Observable<unknown> =>
    isRetryable(err) ? timer(backoffDelay(retryCount, random)) : throwError(() => err);
}

// a small seeded random generator (mulberry32), so the examples print the same delays on every run
export function seededRandom(seed: number): () => number {
  return () => {
    seed = (seed + 0x6d2b79f5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
