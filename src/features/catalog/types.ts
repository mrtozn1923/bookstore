// Shapes returned by Bookstore.Api (System.Text.Json uses camelCase).
export type BookSummary = {
  isbn: string;
  title: string;
  author: string;
  category: string;
  price: number;
  publishedYear: number;
};

export type BookDetails = BookSummary & {
  description: string;
  available: number;
};

export type CategoryView = {
  name: string;
  slug: string;
};

export type PagedResult<T> = {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
};
