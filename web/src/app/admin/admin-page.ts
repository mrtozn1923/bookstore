import { Component } from '@angular/core';
import { books } from '../catalog/books';
import { groupBy } from '../shared/collections';
import { formatPrice } from '../shared/format';

@Component({
  selector: 'app-admin-page',
  template: `
    <h2>Stock value by author</h2>
    @for (row of rows; track row.author) {
      <p>{{ row.author }}: {{ row.value }}</p>
    }
  `,
})
export default class AdminPage {
  protected readonly rows = [...groupBy(books, (book) => book.author)].map(([author, items]) => ({
    author,
    value: formatPrice(items.reduce((sum, book) => sum + book.price, 0)),
  }));
}
