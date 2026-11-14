import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { SearchStore } from './search-store';

@Component({
  selector: 'app-search-box',
  // Component-level store: created with this component, destroyed with it
  providers: [SearchStore],
  template: `
    <input
      class="quick-search"
      placeholder="Quick search"
      [value]="search.query()"
      (input)="search.setQuery($any($event.target).value)"
      (keydown.arrowdown)="search.move(1)"
      (keydown.arrowup)="search.move(-1)"
      (keydown.enter)="open()"
    />
    @if (search.isPending()) {
      <span class="searching">Searching...</span>
    }
    @if (search.requestError(); as error) {
      <span class="search-error">Search failed ({{ error }})</span>
    }
    <ul class="suggestions">
      @for (book of search.results(); track book.isbn; let i = $index) {
        <li [class.highlighted]="i === search.highlighted()">{{ book.title }}</li>
      }
    </ul>
  `,
})
export class SearchBox {
  protected readonly search = inject(SearchStore);
  readonly #router = inject(Router);

  protected open(): void {
    const book = this.search.highlightedItem();
    if (!book) return;
    void this.#router.navigate(['/books', book.isbn]);
    this.search.setQuery('');
  }
}
