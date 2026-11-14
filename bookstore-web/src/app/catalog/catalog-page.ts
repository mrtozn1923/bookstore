import { Component, computed, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { Book } from './book';
import { yieldToMain } from '../core/yield-to-main';

// One formatter for the whole list instead of one per row and change detection
const price = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'TRY' });

@Component({
  selector: 'app-catalog-page',
  imports: [RouterLink, ScrollingModule],
  template: `
    <h1>Catalog</h1>
    <input type="search" class="filter" placeholder="Filter by title or author"
           (input)="onInput($any($event.target).value)" />
    <p>{{ filtered().length }} books</p>
    <!-- Only the rows in (and just around) the viewport exist in the DOM -->
    <cdk-virtual-scroll-viewport itemSize="37" class="books">
      <div *cdkVirtualFor="let book of filtered(); trackBy: byIsbn" class="row">
        <a [routerLink]="['/books', book.isbn]">{{ book.title }}</a>
        <span>{{ book.author }}</span>
        <span>{{ book.publisher }}</span>
        <span>{{ book.year }}</span>
        <span class="price">{{ book.priceText }}</span>
        <span>{{ book.stock > 0 ? 'In stock' : 'Sold out' }}</span>
      </div>
    </cdk-virtual-scroll-viewport>
  `,
  styles: `
    .filter { width: 100%; padding: 10px 12px; font-size: 16px; }
    .books { height: 70vh; font-size: 14px; }
    .row { display: grid; grid-template-columns: 3fr 2fr 2fr 1fr 1fr 1fr; gap: 8px; height: 37px;
           align-items: center; padding: 0 8px; border-bottom: 1px solid #e2e8f0; overflow: hidden; }
    .price { text-align: right; white-space: nowrap; }
  `,
})
export class CatalogPage {
  protected readonly books = httpResource<Book[]>(() => '/api/books');
  protected readonly query = signal('');
  protected readonly byIsbn = (_: number, book: Book) => book.isbn;

  // End the input task here: the list update runs in a new task, the browser can paint before it
  protected async onInput(value: string): Promise<void> {
    await yieldToMain();
    this.query.set(value);
  }

  // Prepared once when the data arrives: lower-cased search text, formatted price, sorted
  private readonly prepared = computed(() =>
    (this.books.value() ?? [])
      .map((b) => ({ ...b, search: `${b.title} ${b.author}`.toLowerCase(), priceText: price.format(b.price) }))
      .sort((a, b) => a.title.localeCompare(b.title)),
  );

  // Recomputed only when query or data change, not on every change detection
  protected readonly filtered = computed(() => {
    const q = this.query().toLowerCase();
    return this.prepared().filter((b) => b.search.includes(q));
  });
}
