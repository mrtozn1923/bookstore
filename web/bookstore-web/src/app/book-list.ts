import { Component, signal } from '@angular/core';

interface Book {
  isbn: string;
  title: string;
  price: number;
}

@Component({
  selector: 'app-book-list',
  template: `
    <ul>
      @for (book of books(); track book.isbn) {
        <li>{{ book.title }} ({{ book.price }} TRY)</li>
      }
    </ul>
  `,
})
export class BookList {
  protected readonly books = signal<Book[]>([]);

  constructor() {
    fetch('/api/books')
      .then((r) => r.json())
      .then((b: Book[]) => this.books.set(b));
  }
}
