import { inject, resource, Service } from '@angular/core';
import { Book } from '../catalog/book';
import { BookstoreDatabase } from '../core/bookstore-database';

@Service()
export class ReadingListStore {
  private readonly database = inject(BookstoreDatabase);

  readonly items = resource({
    loader: async () =>
      (await this.database.connection()).getAllFromIndex('readingList', 'byAddedAt'),
  });

  async add(book: Book) {
    const db = await this.database.connection();
    await db.put('readingList', {
      isbn: book.isbn,
      title: book.title,
      addedAt: new Date().toISOString(),
    });
    this.items.reload();
  }

  async remove(isbn: string) {
    const db = await this.database.connection();
    await db.delete('readingList', isbn);
    this.items.reload();
  }
}
