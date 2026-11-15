import { Component, computed, signal } from '@angular/core';
import { BOOKS, BookFormat } from './book';
import { BookCard } from './book-card';
import { FormatFilter } from './format-filter';
import { Button } from '../ui/button';

@Component({
  selector: 'app-catalog-page',
  imports: [BookCard, FormatFilter, Button],
  template: `
    <main class="mx-auto max-w-5xl p-6">
      <h1 class="mb-4 font-display text-3xl font-bold text-ink-900">Catalog</h1>
      <div class="flex items-center gap-4">
        <app-format-filter [(selected)]="formats" />
        <button
          appButton
          id="clear-filters"
          intent="secondary"
          size="sm"
          class="ml-auto px-2"
          (click)="formats.set([])"
        >
          Clear
        </button>
      </div>
      <p class="my-4 text-sm text-slate-600">{{ visible().length }} of {{ books.length }} books</p>
      <div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
        @for (book of visible(); track book.isbn) {
          <app-book-card [book]="book" />
        }
      </div>
    </main>
  `,
})
export class CatalogPage {
  protected readonly books = BOOKS;
  protected readonly formats = signal<BookFormat[]>([]);
  protected readonly visible = computed(() => {
    const selected = this.formats();
    return selected.length === 0
      ? this.books
      : this.books.filter((b) => selected.includes(b.format));
  });
}
