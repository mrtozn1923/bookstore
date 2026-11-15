import { Link, useLocation } from 'react-router';

export function NotFoundPage() {
  const { pathname } = useLocation();
  return (
    <>
      <h1>Page not found</h1>
      <p>There is nothing at {pathname}.</p>
      <p><Link to="/">Back to the catalog</Link></p>
    </>
  );
}
