// The BFF's JSON, as Angular sees it. Only the fields the screens use.
export interface BookSummary {
  isbn: string;
  title: string;
  author: string;
  category: string;
  price: number;
  currency: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface BookPage extends BookSummary {
  description: string;
  stock: { status: 'InStock' | 'Low' | 'OutOfStock'; available: number } | null;
}

export type OrderStatus = 'Pending' | 'StockReserved' | 'Paid' | 'Confirmed' | 'Cancelled';

export interface Order {
  id: string;
  status: OrderStatus;
  placedAtUtc: string;
  items: { isbn: string; title: string; unitPrice: number; quantity: number; lineTotal: number }[];
  total: number;
  currency: string;
  cancellationReason: string | null;
}

export interface OrderSummary {
  id: string;
  status: OrderStatus;
  placedAtUtc: string;
  itemCount: number;
  total: number;
  currency: string;
}

export interface BffUser {
  name: string;
  email: string;
  roles: string[];
}
