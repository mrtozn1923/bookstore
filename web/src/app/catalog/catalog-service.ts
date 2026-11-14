import { httpResource } from '@angular/common/http';
import { computed, debounced, Injectable, signal } from '@angular/core';
import { Book } from './book';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  readonly category = signal<string | undefined>(undefined);
  readonly search = signal('');
  private readonly term = debounced(() => this.search().trim(), 300);

  readonly books = httpResource<Book[]>(
    () => {
      const category = this.category();
      const search = this.term.value();
      return {
        url: '/api/books',
        params: { ...(category ? { category } : {}), ...(search ? { search } : {}) },
      };
    },
    { defaultValue: [] },
  );

  readonly inStock = computed(() => this.books.value().filter((book) => book.stock > 0));
}
