import { useReducer } from 'react';
import { CartPanel } from '@/features/cart/CartPanel';
import { cartItemCount, cartReducer, cartTotal } from '@/features/cart/cartReducer';
import { CartSummary } from '@/features/cart/CartSummary';
import { CatalogPage } from '@/features/catalog/CatalogPage';
import type { BookSummary } from '@/features/catalog/types';
import { Layout } from '@/shared/Layout';

export default function App() {
  // Lifted state: the header, the catalog and the cart panel all need the cart
  const [lines, dispatch] = useReducer(cartReducer, []);
  const quantities = Object.fromEntries(lines.map((line) => [line.isbn, line.quantity]));

  function handleAdd(book: BookSummary, amount: number) {
    dispatch({ type: 'added', book, amount });
  }

  return (
    <Layout title="Bookstore" headerExtra={<CartSummary itemCount={cartItemCount(lines)} total={cartTotal(lines)} />}>
      <div className="page">
        <div>
          <CatalogPage quantities={quantities} onAdd={handleAdd} />
        </div>
        <CartPanel lines={lines} dispatch={dispatch} />
      </div>
    </Layout>
  );
}
