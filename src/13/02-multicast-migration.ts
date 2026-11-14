import {
  connectable,
  defer,
  interval,
  map,
  publish,
  publishReplay,
  refCount,
  ReplaySubject,
  share,
  shareReplay,
  take,
  tap,
  timer,
  type ConnectableObservable,
  type Observable,
} from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";

// the category list: one request, answered after 200 ms
const categories$ = defer(() => {
  log("-> GET /api/categories");
  return timer(200).pipe(map(() => "classics, fantasy"));
});

function showSharing(shared: Observable<string>) {
  shared.subscribe((list) => log(`nav: ${list}`));
  shared.subscribe((list) => log(`select: ${list}`));
  timer(500).subscribe(() => shared.subscribe((list) => log(`late: ${list}`)));
}

runInVirtualTime("RxJS 6: publishReplay(1) + refCount()", () => {
  showSharing(categories$.pipe(publishReplay(1), refCount()));
});

runInVirtualTime("RxJS 7: shareReplay({ bufferSize: 1, refCount: true })", () => {
  showSharing(categories$.pipe(shareReplay({ bufferSize: 1, refCount: true })));
});

// the same request when the first attempt fails with 503
let attempts = 0;
const flakyCategories$ = defer(() => {
  log("-> GET /api/categories");
  return timer(200).pipe(
    map(() => {
      if (++attempts === 1) throw new Error("HTTP 503");
      return "classics, fantasy";
    }),
  );
});

function showRecovery(shared: Observable<string>) {
  attempts = 0;
  const report = (label: string) => ({
    next: (list: string) => log(`${label}: ${list}`),
    error: (err: Error) => log(`${label} failed: ${err.message}`),
  });
  shared.subscribe(report("nav"));
  timer(500).subscribe(() => shared.subscribe(report("late")));
}

runInVirtualTime("after an error, RxJS 6: publishReplay(1) + refCount()", () => {
  showRecovery(flakyCategories$.pipe(publishReplay(1), refCount()));
});

runInVirtualTime("after an error, RxJS 7: shareReplay({ bufferSize: 1, refCount: true })", () => {
  showRecovery(flakyCategories$.pipe(shareReplay({ bufferSize: 1, refCount: true })));
});

// the exact replacement from the RxJS deprecation guide: never reset, keep the error too
runInVirtualTime("after an error, RxJS 7: share() with a ReplaySubject, no resets", () => {
  showRecovery(
    flakyCategories$.pipe(
      share({
        connector: () => new ReplaySubject(1),
        resetOnError: false,
        resetOnComplete: false,
        resetOnRefCountZero: false,
      }),
    ),
  );
});

// Dune's stock, three polls one second apart
const stock$ = interval(1000).pipe(
  take(3),
  tap((i) => log(`-> GET /api/books/9780441172719/stock (poll ${i + 1})`)),
);

runInVirtualTime("RxJS 6: publish() + connect()", () => {
  const shared = stock$.pipe(publish()) as ConnectableObservable<number>;
  shared.subscribe((i) => log(`screen: poll ${i + 1}`));
  shared.subscribe((i) => log(`badge: poll ${i + 1}`));
  shared.connect();
});

runInVirtualTime("RxJS 7: connectable() + connect()", () => {
  const shared = connectable(stock$);
  shared.subscribe((i) => log(`screen: poll ${i + 1}`));
  shared.subscribe((i) => log(`badge: poll ${i + 1}`));
  shared.connect();
});
