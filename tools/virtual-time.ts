// Runs time-based examples in RxJS's virtual time (TestScheduler run mode): timer, delay, debounceTime…
// use virtual milliseconds, so a 5-second scenario finishes instantly and prints the same times on every run.
import { map, merge, timer, type Observable } from "rxjs";
import { TestScheduler } from "rxjs/testing";

let scheduler: TestScheduler | undefined;

export function runInVirtualTime(title: string, scenario: () => void): void {
  console.log(`--- ${title}`);
  scheduler = new TestScheduler(() => {});
  scheduler.run(() => scenario());
  scheduler = undefined;
}

// prints a line with the current virtual time
export function log(message: string): void {
  console.log(`${String(scheduler?.now() ?? 0).padStart(5)} ms  ${message}`);
}

// emits each value at the given virtual time, then completes after the last one
export function emitAt<T>(...events: [ms: number, value: T][]): Observable<T> {
  return merge(...events.map(([ms, value]) => timer(ms).pipe(map(() => value))));
}
