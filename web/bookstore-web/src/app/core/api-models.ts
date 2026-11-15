// Shapes returned by Bookstore.Api (System.Text.Json uses camelCase).

// Mirrors OrderService.InstitutionalMinimumQuantity; the API enforces the rule, the client only explains it.
export const institutionalMinimumQuantity = 20;
export interface BookSummary {
  isbn: string;
  title: string;
  author: string;
  category: string;
  price: number;
  publishedYear: number;
}

export interface BookDetails extends BookSummary {
  description: string;
  available: number;
  // null when the ISBN-13 has no ISBN-10 (979 prefix)
  isbn10: string | null;
}

export interface CategoryView {
  name: string;
  slug: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CartLine {
  isbn: string;
  title: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface CartView {
  id: string;
  items: CartLine[];
  total: number;
  currency: string;
}

export interface OrderView {
  id: number;
  status: string;
  placedAtUtc: string;
  items: { title: string; unitPrice: number; quantity: number; lineTotal: number }[];
  total: number;
  currency: string;
}
