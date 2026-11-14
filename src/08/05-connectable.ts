import { connect, connectable, defer, filter, interval, map, merge, take, timer } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";

// Dune's stock feed: logs every connection, emits 12, 11, 10 one second apart
const stockFeed = defer(() => {
  log("feed connected");
  return interval(1000).pipe(
    map((i) => 12 - i),
    take(3),
  );
});

runInVirtualTime("connectable: subscribe first, connect when ready", () => {
  const stock$ = connectable(stockFeed);
  stock$.subscribe((s) => log(`header badge: ${s}`));
  stock$.subscribe((s) => log(`detail page: ${s}`));
  timer(500).subscribe(() => {
    log("connect()");
    stock$.connect(); // one subscription to the feed, for both subscribers
  });
});

runInVirtualTime("connect operator: share inside one pipe", () => {
  stockFeed
    .pipe(
      connect((shared$) =>
        merge(
          shared$.pipe(map((s) => `stock: ${s}`)),
          shared$.pipe(
            filter((s) => s <= 10),
            map((s) => `low stock warning: ${s}`),
          ),
        ),
      ),
    )
    .subscribe((line) => log(line));
});
