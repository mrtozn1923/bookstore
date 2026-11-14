import { Component } from '@angular/core';
import _ from 'lodash';
import { books } from '../catalog/books';
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
  protected readonly rows = _.map(_.groupBy(books, 'author'), (items, author) => ({
    author,
    value: formatPrice(_.sumBy(items, 'price')),
  }));
}
