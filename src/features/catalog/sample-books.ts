import type { BookSummary } from '@/features/catalog/types';

// A few books from the Bookstore.Api seed; replaced by a real API call later in the series
export const sampleBooks: BookSummary[] = [
  { isbn: '9780441172719', title: 'Dune', author: 'Frank Herbert', category: 'Science Fiction', price: 489, publishedYear: 1965 },
  { isbn: '9780261102217', title: 'The Hobbit', author: 'J.R.R. Tolkien', category: 'Fantasy', price: 329.9, publishedYear: 1937 },
  { isbn: '9780141439587', title: 'Emma', author: 'Jane Austen', category: 'Classics', price: 249.9, publishedYear: 1815 },
  { isbn: '9780141439549', title: 'Middlemarch', author: 'George Eliot', category: 'Classics', price: 389, publishedYear: 1871 },
  { isbn: '9780451524935', title: '1984', author: 'George Orwell', category: 'Dystopian', price: 279, publishedYear: 1949 },
];
