import { notFound } from 'next/navigation';
import { Suspense } from 'react';
import { getBook, getBooksByAuthor, getStock } from '@/lib/bookstoreApi';
import { AddToCartForm } from './AddToCartForm';
import { MoreByAuthor } from './MoreByAuthor';

// Prerendered at build time; any other ISBN is rendered on its first request
export async function generateStaticParams() {
  return [{ isbn: '9780441172719' }, { isbn: '9780141439587' }];
}

// A Server Component: async, runs only on the server, its code never reaches the browser
export default async function BookPage({ params }: PageProps<'/books/[isbn]'>) {
  const { isbn } = await params;
  const book = await getBook(isbn);
  if (book === null) {
    notFound();
  }
  // Not awaited: the promise travels to the client component, which waits for it with use()
  const moreByAuthor = getBooksByAuthor(book.author).then((books) => books.filter((b) => b.isbn !== book.isbn));

  return (
    <article>
      <h1>{book.title}</h1>
      <p>{book.author}, {book.publishedYear} · {book.category} · {book.price.toFixed(2)} TRY</p>
      <p>{book.description}</p>
      <Suspense fallback={<p className="muted">Checking stock…</p>}>
        <Stock isbn={isbn} />
      </Suspense>
      <AddToCartForm isbn={book.isbn} />
      <Suspense fallback={<p className="muted">Loading more books…</p>}>
        <MoreByAuthor author={book.author} books={moreByAuthor} />
      </Suspense>
    </article>
  );
}

async function Stock({ isbn }: { isbn: string }) {
  const available = await getStock(isbn);
  return <p className="muted">{available > 0 ? `${available} in stock` : 'Out of stock'}</p>;
}
