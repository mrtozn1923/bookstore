import type { BookDetails } from '@/features/catalog';

// A few real rows from the API seed; enough for search, paging and the cart
export const testBooks: BookDetails[] = [
  { isbn: '9780441172719', title: 'Dune', author: 'Frank Herbert', category: 'Science Fiction', price: 489, publishedYear: 1965,
    description: 'A desert planet, a noble family and the most valuable substance in the universe.', available: 25 },
  { isbn: '9780141439587', title: 'Emma', author: 'Jane Austen', category: 'Classics', price: 249.9, publishedYear: 1815,
    description: 'A young woman in a small English village keeps arranging other people\'s marriages.', available: 25 },
  { isbn: '9780141439518', title: 'Pride and Prejudice', author: 'Jane Austen', category: 'Classics', price: 239.9, publishedYear: 1813,
    description: 'Elizabeth Bennet and Mr Darcy misjudge each other, then slowly change their minds.', available: 25 },
  { isbn: '9780140449266', title: 'Don Quixote', author: 'Miguel de Cervantes', category: 'Classics', price: 399, publishedYear: 1605,
    description: 'A man who has read too many chivalric romances sets out to become a knight.', available: 0 },
];
