import { EMPTY, expand, reduce } from "rxjs";
import { books } from "../../data/books.ts";
import { log, runInVirtualTime } from "../../tools/virtual-time.ts";
import { fakeRequest } from "./fake-api.ts";

type CatalogPage = { items: string[]; nextOffset: number | null };

// the catalog API returns 2 books per page and tells us where the next page starts
function getCatalogPage(offset: number) {
  return fakeRequest(`GET /books?offset=${offset}&limit=2`, 100, (): CatalogPage => ({
    items: books.slice(offset, offset + 2).map((b) => b.title),
    nextOffset: offset + 2 < books.length ? offset + 2 : null,
  }));
}

runInVirtualTime("expand", () => {
  getCatalogPage(0)
    .pipe(
      expand((page) => (page.nextOffset === null ? EMPTY : getCatalogPage(page.nextOffset))),
      reduce((all, page) => [...all, ...page.items], [] as string[]),
    )
    .subscribe((titles) => log(`${titles.length} books: ${titles.join(", ")}`));
});
