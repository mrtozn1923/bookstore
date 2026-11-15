import { RenderMode, ServerRoute } from '@angular/ssr';
import { BOOKS } from './books/book';

export const serverRoutes: ServerRoute[] = [
  {
    path: 'books/:slug',
    renderMode: RenderMode.Prerender,
    getPrerenderParams: async () => BOOKS.map((b) => ({ slug: b.slug })),
  },
  { path: '**', renderMode: RenderMode.Server },
];
