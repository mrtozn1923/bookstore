// Filters the shared sample books like Bookstore's catalog endpoint would (virtual-time fake).
import { books, type Category } from "../../data/books.ts";
import { fakeRequest } from "../04/fake-api.ts";

export type PriceRange = { min: number; max: number };
export type SortOrder = "title" | "price-asc" | "price-desc";
export type CatalogQuery = { category: Category | "all"; priceRange: PriceRange; sort: SortOrder };

export function toQueryString(q: CatalogQuery): string {
  return `category=${q.category}&min=${q.priceRange.min}&max=${q.priceRange.max}&sort=${q.sort}`;
}

export function getBooks(q: CatalogQuery) {
  return fakeRequest(`GET /books?${toQueryString(q)}`, 200, () =>
    books
      .filter((b) => q.category === "all" || b.category === q.category)
      .filter((b) => b.price >= q.priceRange.min && b.price <= q.priceRange.max)
      .toSorted((a, b) =>
        q.sort === "title" ? a.title.localeCompare(b.title) : q.sort === "price-asc" ? a.price - b.price : b.price - a.price,
      )
      .map((b) => `${b.title} ${b.price}`),
  );
}
