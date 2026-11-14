import { interval, map, shareReplay, tap, timer } from "rxjs";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";

// a live stock feed for Dune that never completes
const liveStock = () =>
  interval(1000).pipe(
    map((i) => 12 - i),
    tap((stock) => log(`  feed produced ${stock}`)),
  );

for (const refCount of [false, true]) {
  runInVirtualTime(`shareReplay({ bufferSize: 1, refCount: ${refCount} })`, () => {
    const stock$ = liveStock().pipe(shareReplay({ bufferSize: 1, refCount }));
    const detailPage = stock$.subscribe((s) => log(`detail page: ${s}`));
    timer(2500).subscribe(() => {
      detailPage.unsubscribe();
      log("user leaves the detail page");
    });
  }, 5000);
}
