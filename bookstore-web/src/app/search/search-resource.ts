import { httpResource } from '@angular/common/http';
import { Component, debounced, signal } from '@angular/core';
import { apiUrl } from '../api';
import type { Book } from '../catalog/book';

// 3. no RxJS: debounced() (experimental in Angular 22) and httpResource
@Component({
  selector: 'app-search-resource',
  template: `
    <input id="search-resource" #box [value]="term()" (input)="term.set(box.value)" />
    @if (results.isLoading()) {
      <p>Searching...</p>
    }
    <ul>
      @for (book of results.value(); track book.isbn) {
        <li>{{ book.title }}</li>
      }
    </ul>
  `,
})
export class SearchResource {
  protected readonly term = signal('');
  readonly #settledTerm = debounced(this.term, 300);

  protected readonly results = httpResource<Book[]>(
    () => {
      const term = this.#settledTerm.value().trim();
      return term ? `${apiUrl}/books?q=${encodeURIComponent(term)}` : undefined;
    },
    { defaultValue: [] },
  );
}
