// Fake HTTP calls that can fail, for virtual-time error handling examples.
import { defer, Observable, timer } from "rxjs";
import { log } from "../../tools/virtual-time.ts";

export class HttpError extends Error {
  readonly status: number;

  constructor(status: number) {
    super(`HTTP ${status}`);
    this.status = status;
    this.name = "HttpError";
  }
}

// one call: after latencyMs it answers with the value (status 200) or fails with HttpError(status)
export function fakeHttp<T>(label: string, latencyMs: number, status: number, answer: () => T): Observable<T> {
  return new Observable<T>((subscriber) => {
    log(`-> ${label}`);
    const pending = timer(latencyMs).subscribe(() => {
      log(`<- ${label} ${status}`);
      if (status === 200) {
        subscriber.next(answer());
        subscriber.complete();
      } else {
        subscriber.error(new HttpError(status));
      }
    });
    return () => pending.unsubscribe();
  });
}

// every new subscription (e.g. a retry) takes the next status from the list; the last one repeats
export function flakyHttp<T>(label: string, latencyMs: number, statuses: number[], answer: () => T): Observable<T> {
  let calls = 0;
  return defer(() => fakeHttp(label, latencyMs, statuses[Math.min(calls++, statuses.length - 1)], answer));
}
