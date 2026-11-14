import { combineLatest, NEVER, timer, type Observable } from "rxjs";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import type { PriceRange } from "./catalog-api.ts";

runInVirtualTime("catalog filters", () => {
  const category$ = emitAt([100, "classics"]);
  const sort$ = emitAt([300, "price-asc"]);
  const priceRange$: Observable<PriceRange> = NEVER; // the user never touches the price slider

  combineLatest({ category: category$, sort: sort$, priceRange: priceRange$ }).subscribe((filters) =>
    log(`query: ${JSON.stringify(filters)}`),
  );
  timer(1000).subscribe(() => log("one second later"));
});
