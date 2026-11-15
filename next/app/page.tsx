import Link from 'next/link';
import { getCatalogPage } from '@/lib/bookstoreApi';

export default async function CatalogPage() {
  const books = await getCatalogPage();
  return (
    <>
      <h1>Catalog</h1>
      <ul>
        {books.map((book) => (
          <li key={book.isbn}><Link href={`/books/${book.isbn}`}>{book.title}</Link> <span className="muted">{book.author}</span></li>
        ))}
      </ul>
    </>
  );
}
