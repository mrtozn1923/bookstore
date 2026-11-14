import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  { path: '', renderMode: RenderMode.Prerender },
  // SSR: every request renders the book page on the server (2,000 books, prices and stock change)
  { path: 'books/:isbn', renderMode: RenderMode.Server },
  { path: '**', renderMode: RenderMode.Client },
];
