import { defer, exhaustMap, mergeMap, repeat, switchMap, takeWhile, timer, type Observable } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { statusApi, type Status } from "./status-api.ts";

function poll(title: string, build: (getStatus: () => Observable<Status>) => Observable<Status>) {
  runInVirtualTime(title, () => {
    // the second call is slow (6.5 s), the others take 300 ms
    build(statusApi([300, 6500]))
      .pipe(takeWhile((status) => status !== "Delivered", true)) // stop after Delivered
      .subscribe((status) => log(`screen: ${status}`));
  });
}

poll("timer + mergeMap", (getStatus) => timer(0, 5000).pipe(mergeMap(() => getStatus())));
poll("timer + switchMap", (getStatus) => timer(0, 5000).pipe(switchMap(() => getStatus())));
poll("timer + exhaustMap", (getStatus) => timer(0, 5000).pipe(exhaustMap(() => getStatus())));
poll("defer + repeat({ delay })", (getStatus) => defer(() => getStatus()).pipe(repeat({ delay: 5000 })));
