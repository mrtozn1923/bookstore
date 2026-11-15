// Test data for the back-office stock page: 5,000 stock lots built from the 20 catalog books.
// The API copy has 20 books; a generated list makes render costs measurable.
export type StockLot = {
  lotId: string;
  isbn: string;
  title: string;
  author: string;
  warehouse: string;
  onHand: number;
};

const books: [isbn: string, title: string, author: string][] = [
  ['9780441172719', 'Dune', 'Frank Herbert'], ['9780261102217', 'The Hobbit', 'J.R.R. Tolkien'],
  ['9780141439587', 'Emma', 'Jane Austen'], ['9780141439549', 'Middlemarch', 'George Eliot'],
  ['9780141439518', 'Pride and Prejudice', 'Jane Austen'], ['9780451524935', '1984', 'George Orwell'],
  ['9780060850524', 'Brave New World', 'Aldous Huxley'], ['9780141439471', 'Frankenstein', 'Mary Shelley'],
  ['9780142437247', 'Moby-Dick', 'Herman Melville'], ['9780141441146', 'Jane Eyre', 'Charlotte Brontë'],
  ['9780141439556', 'Wuthering Heights', 'Emily Brontë'], ['9780141439563', 'Great Expectations', 'Charles Dickens'],
  ['9780143058144', 'Crime and Punishment', 'Fyodor Dostoevsky'], ['9780143035008', 'Anna Karenina', 'Leo Tolstoy'],
  ['9781400079988', 'War and Peace', 'Leo Tolstoy'], ['9780743273565', 'The Great Gatsby', 'F. Scott Fitzgerald'],
  ['9780553293357', 'Foundation', 'Isaac Asimov'], ['9780441478125', 'The Left Hand of Darkness', 'Ursula K. Le Guin'],
  ['9780140449266', 'The Count of Monte Cristo', 'Alexandre Dumas'], ['9780060934347', 'Don Quixote', 'Miguel de Cervantes'],
];
const warehouses = ['Istanbul', 'Ankara', 'Izmir'];

export function makeStockLots(count: number): StockLot[] {
  return Array.from({ length: count }, (_, i) => {
    const [isbn, title, author] = books[i % books.length];
    return { lotId: `LOT-${String(i + 1).padStart(5, '0')}`, isbn, title, author, warehouse: warehouses[i % warehouses.length], onHand: (i * 7) % 40 };
  });
}
