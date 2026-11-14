import { Book } from './book';

export const books: readonly Book[] = [
  { isbn: '9780441172719', title: 'Dune', author: 'Frank Herbert', price: 420, addedAt: '2026-11-10T09:00:00Z' },
  { isbn: '9780141439587', title: 'Emma', author: 'Jane Austen', price: 260, addedAt: '2026-10-02T09:00:00Z' },
  { isbn: '9780547928227', title: 'The Hobbit', author: 'J. R. R. Tolkien', price: 340, addedAt: '2026-11-13T09:00:00Z' },
  { isbn: '9780141439549', title: 'Middlemarch', author: 'George Eliot', price: 390, addedAt: '2026-08-21T09:00:00Z' },
  { isbn: '9780141439518', title: 'Pride and Prejudice', author: 'Jane Austen', price: 250, addedAt: '2026-09-15T09:00:00Z' },
  { isbn: '9780141439976', title: 'Silas Marner', author: 'George Eliot', price: 210, addedAt: '2026-07-30T09:00:00Z' },
  { isbn: '9780141439686', title: 'Persuasion', author: 'Jane Austen', price: 230, addedAt: '2026-11-01T09:00:00Z' },
  { isbn: '9780141439471', title: 'Frankenstein', author: 'Mary Shelley', price: 240, addedAt: '2026-10-25T09:00:00Z' },
];

export function findBook(isbn: string): Book | undefined {
  return books.find((book) => book.isbn === isbn);
}
