import { ReplaySubject, Subject, timer } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";

// the warehouse example from the creation functions post, now with Subjects
runInVirtualTime("Subject: B misses earlier events", () => {
  const restocked = new Subject<string>();
  timer(100).subscribe(() => restocked.next("Dune"));
  timer(300).subscribe(() => restocked.next("Emma"));
  restocked.subscribe((title) => log(`A: ${title} restocked`));
  timer(250).subscribe(() => restocked.subscribe((title) => log(`B: ${title} restocked`)));
});

runInVirtualTime("ReplaySubject(1): B gets the last event, even when it is stale", () => {
  const restocked = new ReplaySubject<string>(1);
  const soldOut = new Subject<string>();
  timer(100).subscribe(() => restocked.next("Dune"));
  timer(200).subscribe(() => soldOut.next("Dune")); // Dune sells out again, restocked$ does not know
  soldOut.subscribe((title) => log(`warehouse: ${title} sold out`));
  timer(250).subscribe(() => restocked.subscribe((title) => log(`B: ${title} restocked (replayed)`)));
});
