import { isRouteErrorResponse, Link, useRouteError } from 'react-router';

// errorElement of the root route: loader errors, thrown responses and render errors end up here
export function RouteErrorPage() {
  const error = useRouteError();
  const title = isRouteErrorResponse(error) ? `${error.status} ${error.statusText}` : 'Something went wrong';
  const detail = isRouteErrorResponse(error) ? String(error.data) : error instanceof Error ? error.message : '';

  return (
    <main>
      <h1>{title}</h1>
      <p>{detail}</p>
      <p><Link to="/">Back to the catalog</Link></p>
    </main>
  );
}
