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
