export type BookFormat = 'Hardcover' | 'Paperback' | 'E-book';

export interface Book {
  isbn: string;
  title: string;
  author: string;
  price: number;
  format: BookFormat;
  stock: number;
}

export const BOOKS: readonly Book[] = [
  {
    isbn: '9780441172719',
    title: 'Dune',
    author: 'Frank Herbert',
    price: 349,
    format: 'Paperback',
    stock: 12,
  },
  {
    isbn: '9780547928227',
    title: 'The Hobbit',
    author: 'J. R. R. Tolkien',
    price: 289,
    format: 'Hardcover',
    stock: 3,
  },
  {
    isbn: '9780141439587',
    title: 'Emma',
    author: 'Jane Austen',
    price: 189,
    format: 'Paperback',
    stock: 0,
  },
  {
    isbn: '9780141439549',
    title: 'Middlemarch',
    author: 'George Eliot',
    price: 239,
    format: 'E-book',
    stock: 99,
  },
];
