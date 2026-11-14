// A fake HTTP call for virtual-time examples: logs the request, answers after latencyMs,
// and logs a cancellation if the subscriber unsubscribes before the answer.
// onServer (optional) is what the server does as soon as the request arrives.
import { Observable, timer } from "rxjs";
import { log } from "../../tools/virtual-time.ts";

export function fakeRequest<T>(label: string, latencyMs: number, answer: () => T, onServer?: () => void): Observable<T> {
  return new Observable<T>((subscriber) => {
    log(`-> ${label}`);
    onServer?.();
    let answered = false;
    const pending = timer(latencyMs).subscribe(() => {
      answered = true;
      log(`<- ${label}`);
      subscriber.next(answer());
      subscriber.complete();
    });
    return () => {
      if (!answered) log(`x  ${label} cancelled`);
      pending.unsubscribe();
    };
  });
}
