// Same shape as BookSummary returned by Bookstore.Api (GET /api/books).
export type BookSummary = {
  isbn: string;
  title: string;
  author: string;
  category: string;
  price: number;
  publishedYear: number;
};

type BookCardProps = {
  book: BookSummary;
  quantity: number;
  onAdd: (isbn: string) => void;
};

const price = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'TRY', currencyDisplay: 'narrowSymbol' });

export function BookCard({ book, quantity, onAdd }: BookCardProps) {
  // A derived value: computed on every render, never stored
  const label = quantity === 0 ? 'Add to cart' : `In cart: ${quantity}`;

  return (
    <li className="book-card">
      <strong>{book.title}</strong>
      <span className="muted">{book.author} · {book.publishedYear}</span>
      <strong>{price.format(book.price)}</strong>
      <button type="button" onClick={() => onAdd(book.isbn)}>{label}</button>
    </li>
  );
}
