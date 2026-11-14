export interface Book {
  isbn: string;
  title: string;
  author: string;
  publisher: string;
  year: number;
  price: number;
  currency: string;
  stock: number;
  category: string;
  coverUrl: string | null;
  summary: string;
}
