import { Component, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { Book } from './book';

@Component({
  selector: 'app-catalog-page',
  imports: [RouterLink],
  template: `
    <h1>Catalog</h1>
    <input type="search" class="filter" placeholder="Filter by title or author"
           (input)="query.set($any($event.target).value)" />
    <p>{{ filtered().length }} books</p>
    <table class="books">
      <tbody>
        @for (book of filtered(); track $index) {
          <tr>
            <td><a [routerLink]="['/books', book.isbn]">{{ book.title }}</a></td>
            <td>{{ book.author }}</td>
            <td>{{ book.publisher }}</td>
            <td>{{ book.year }}</td>
            <td class="price">{{ formatPrice(book) }}</td>
            <td>{{ book.stock > 0 ? 'In stock' : 'Sold out' }}</td>
          </tr>
        }
      </tbody>
    </table>
  `,
  styles: `
    .filter { width: 100%; padding: 10px 12px; font-size: 16px; }
    .books { width: 100%; border-collapse: collapse; font-size: 14px; }
    .books td { padding: 6px 8px; border-bottom: 1px solid #e2e8f0; }
    .price { text-align: right; white-space: nowrap; }
  `,
})
export class CatalogPage {
  protected readonly books = httpResource<Book[]>(() => '/api/books');
  protected readonly query = signal('');

  protected filtered(): Book[] {
    const q = this.query().toLocaleLowerCase('en');
    return (this.books.value() ?? [])
      .filter((b) => `${b.title} ${b.author}`.toLocaleLowerCase('en').includes(q))
      .sort((a, b) => a.title.localeCompare(b.title));
  }

  protected formatPrice(book: Book): string {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: book.currency }).format(book.price);
  }
}
