'use client';

import Link from 'next/link';
import { use, useState } from 'react';

type MoreByAuthorProps = {
  author: string;
  // A promise created on the server; React streams its result to the browser
  books: Promise<{ isbn: string; title: string }[]>;
};

export function MoreByAuthor({ author, books }: MoreByAuthorProps) {
  const list = use(books);
  const [showAll, setShowAll] = useState(false);
  if (list.length === 0) {
    return null;
  }
  const shown = showAll ? list : list.slice(0, 1);
  return (
    <section>
      <h2>More by {author}</h2>
      <ul>
        {shown.map((b) => <li key={b.isbn}><Link href={`/books/${b.isbn}`}>{b.title}</Link></li>)}
      </ul>
      {list.length > 1 && !showAll && <button type="button" onClick={() => setShowAll(true)}>Show all {list.length}</button>}
    </section>
  );
}
