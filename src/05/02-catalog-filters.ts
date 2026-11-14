import { combineLatest, distinctUntilChanged, map, startWith, switchMap } from "rxjs";
import { emitAt, log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { getBooks, toQueryString, type CatalogQuery, type PriceRange, type SortOrder } from "./catalog-api.ts";

runInVirtualTime("combineLatest + startWith", () => {
  // what the user does on the catalog page, and when
  const category$ = emitAt<CatalogQuery["category"]>([100, "classics"]);
  const priceRange$ = emitAt<PriceRange>([500, { min: 0, max: 350 }]);
  const sort$ = emitAt<SortOrder>([900, "price-asc"]);

  const query$ = combineLatest({
    category: category$.pipe(startWith<CatalogQuery["category"]>("all")),
    priceRange: priceRange$.pipe(startWith({ min: 0, max: 1000 })),
    sort: sort$.pipe(startWith<SortOrder>("title")),
  });

  query$
    .pipe(
      distinctUntilChanged((a, b) => toQueryString(a) === toQueryString(b)),
      switchMap((query) => getBooks(query)),
      map((titles) => titles.join(", ")),
    )
    .subscribe((list) => log(`screen: ${list}`));
});
