// Shapes returned by Bookstore.Api (System.Text.Json uses camelCase).
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
