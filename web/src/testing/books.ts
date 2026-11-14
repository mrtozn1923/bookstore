import { Book } from '../app/catalog/book';

export const dune: Book = { id: 1, isbn: '9780441013593', title: 'Dune', author: 'Frank Herbert', category: 'sci-fi', price: 450, stock: 3 };
export const emma: Book = { id: 2, isbn: '9780141439587', title: 'Emma', author: 'Jane Austen', category: 'classics', price: 320, stock: 0 };
export const hobbit: Book = { id: 3, isbn: '9780261103344', title: 'The Hobbit', author: 'J. R. R. Tolkien', category: 'fantasy', price: 380, stock: 5 };

const classics = [
  'Dune', 'Emma', 'The Hobbit', 'Middlemarch', 'Persuasion', 'Ulysses', 'Dracula', 'Frankenstein', 'Beloved', 'Walden',
  'Hamlet', 'Macbeth', 'Othello', 'Candide', 'Ivanhoe', 'Rebecca', 'Moby-Dick', 'Jane Eyre', 'Great Expectations',
  'War and Peace', 'Anna Karenina', 'The Odyssey', 'The Iliad', 'Don Quixote', 'Madame Bovary', 'Little Women',
  'Wuthering Heights', 'Heart of Darkness', 'Brave New World', 'North and South',
];

export function catalogOf(count: number): Book[] {
  return classics.slice(0, count).map((title, index) => ({
    id: 100 + index,
    isbn: `9780000000${String(index).padStart(3, '0')}`,
    title,
    author: 'Various',
    category: 'classics',
    price: 200 + index * 10,
    stock: 5,
  }));
}
