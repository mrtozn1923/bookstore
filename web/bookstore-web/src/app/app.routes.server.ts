import { RenderMode, ServerRoute } from '@angular/ssr';
import { BOOKS } from './books/book';

export const serverRoutes: ServerRoute[] = [
  // filtre ve sayfa kombinasyonları önceden bilinemez: her istek sunucuda render edilir
  { path: 'books/genre/**', renderMode: RenderMode.Server },
  {
    path: 'books/:slug',
    renderMode: RenderMode.Prerender,
    getPrerenderParams: async () => BOOKS.map((b) => ({ slug: b.slug })),
  },
  { path: '**', renderMode: RenderMode.Server },
];
