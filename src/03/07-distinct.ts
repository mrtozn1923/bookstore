import { distinctUntilChanged, distinctUntilKeyChanged, map } from "rxjs";
import { showMarbles } from "../../tools/marble.ts";

// search box: the same term again should not send a new request
showMarbles("distinctUntilChanged() on search terms", (cold) =>
  cold("-a--a--b--b--a--|", { a: "dune", b: "emma" }).pipe(distinctUntilChanged()),
);

type CatalogFilter = { category: string; maxPrice: number };
const filters: Record<string, CatalogFilter> = {
  a: { category: "classics", maxPrice: 400 },
  b: { category: "classics", maxPrice: 400 }, // same content, new object
  c: { category: "classics", maxPrice: 300 },
};

// 1) compare the fields yourself
showMarbles("distinctUntilChanged(sameFilter)", (cold) =>
  cold("-a--b--c--|", filters).pipe(
    distinctUntilChanged((prev, curr) => prev.category === curr.category && prev.maxPrice === curr.maxPrice),
  ),
);

// 2) only one field matters
showMarbles("distinctUntilKeyChanged('category')", (cold) =>
  cold("-a--b--c--|", filters).pipe(distinctUntilKeyChanged("category")),
);

// 3) turn the filter into a primitive first, e.g. the query string it produces
showMarbles("map(toQuery), distinctUntilChanged()", (cold) =>
  cold("-a--b--c--|", filters).pipe(
    map((f) => `category=${f.category}&maxPrice=${f.maxPrice}`),
    distinctUntilChanged(),
  ),
);
