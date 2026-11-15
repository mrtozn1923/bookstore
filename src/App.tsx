import { useState } from 'react';
import { CartSummary } from '@/features/cart/CartSummary';
import { BookList } from '@/features/catalog/BookList';
import { sampleBooks } from '@/features/catalog/sample-books';
import { Layout } from '@/shared/Layout';

export default function App() {
  // isbn -> quantity; the next post explains why we always build a new object
  const [quantities, setQuantities] = useState<Record<string, number>>({});
  const itemCount = Object.values(quantities).reduce((sum, q) => sum + q, 0);

  function handleAdd(isbn: string) {
    setQuantities((current) => ({ ...current, [isbn]: (current[isbn] ?? 0) + 1 }));
  }

  return (
    <Layout title="Bookstore" headerExtra={<CartSummary itemCount={itemCount} />}>
      <h1>Catalog</h1>
      <BookList books={sampleBooks} quantities={quantities} onAdd={handleAdd} />
    </Layout>
  );
}
