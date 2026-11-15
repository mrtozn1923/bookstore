import { Link } from 'react-router';
import { useClientStore } from '@/shared/clientStore';

export function RecentlyViewed() {
  const recent = useClientStore((state) => state.recentlyViewed);
  const clear = useClientStore((state) => state.clearRecentlyViewed);

  if (recent.length === 0) {
    return null;
  }
  return (
    <aside className="recent" aria-label="Recently viewed">
      <h2>Recently viewed</h2>
      <ul>
        {recent.map((book) => <li key={book.isbn}><Link to={`/books/${book.isbn}`}>{book.title}</Link></li>)}
      </ul>
      <button type="button" className="link" onClick={clear}>Clear history</button>
    </aside>
  );
}
