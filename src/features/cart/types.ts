// Same shapes as CartView/CartLineView/OrderView in Bookstore.Api
export type CartLine = {
  isbn: string;
  title: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
};

export type Cart = {
  id: string;
  items: CartLine[];
  total: number;
  currency: string;
};

export type Order = {
  id: number;
  status: string;
  placedAtUtc: string;
  total: number;
  currency: string;
};
