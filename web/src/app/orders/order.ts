export type OrderStatus = 'Placed' | 'Shipped' | 'Cancelled';

export interface OrderLine {
  isbn: string;
  title: string;
  quantity: number;
  price: number;
}

export interface Order {
  id: number;
  reference: string;
  status: OrderStatus;
  total: number;
  lines: OrderLine[];
  createdAt: string;
}

export interface PlaceOrderRequest {
  fullName: string;
  email: string;
  city: string;
  lines: { isbn: string; quantity: number }[];
}
