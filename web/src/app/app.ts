import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Book, BookCatalogService } from './book-catalog';
import { RuntimeConfigService } from './runtime-config';

@Component({
  imports: [CurrencyPipe],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly environment = inject(RuntimeConfigService).value.environment;
  protected readonly books = signal<Book[]>([]);
  protected readonly error = signal<string | null>(null);

  constructor() {
    inject(BookCatalogService)
      .getBooks()
      .subscribe({
        next: (books) => this.books.set(books),
        error: () => this.error.set('Catalog is not available'),
      });
  }
}
