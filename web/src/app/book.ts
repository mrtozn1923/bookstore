export interface Book {
  isbn: string;
  title: string;
  author: string;
  price: number;
  stock: number;
}

export const BOOKS: Book[] = [
  { isbn: '9780441172719', title: 'Dune', author: 'Frank Herbert', price: 450, stock: 12 },
  { isbn: '9780141439587', title: 'Emma', author: 'Jane Austen', price: 220, stock: 3 },
  { isbn: '9780547928227', title: 'The Hobbit', author: 'J. R. R. Tolkien', price: 380, stock: 0 },
  { isbn: '9780141439549', title: 'Middlemarch', author: 'George Eliot', price: 310, stock: 7 },
];
