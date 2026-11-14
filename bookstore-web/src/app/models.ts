export interface Book {
  isbn: string;
  title: string;
  author: string;
  price: number;
  stock: number;
  category: string;
}

export interface BookPage {
  items: Book[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CartItem {
  isbn: string;
  title: string;
  price: number;
  quantity: number;
}

export interface Category {
  slug: string;
  name: string;
}

export interface Review {
  id: number;
  isbn: string;
  author: string;
  rating: number;
  text: string;
}

export interface Coupon {
  code: string;
  percent: number;
}

export interface OrderRequest {
  items: CartItem[];
  shipping: 'standard' | 'express';
  total: number;
}

export interface PlacedOrder extends OrderRequest {
  id: number;
}

// An order as the back office sees it (classic NgRx Store, post 08)
export interface AdminOrder {
  id: number;
  customer: string;
  items: { isbn: string; title: string; quantity: number }[];
  total: number;
  status: OrderStatus;
}

export type OrderStatus = 'new' | 'shipped';
