import { Service, signal } from '@angular/core';
import { DBSchema, IDBPDatabase, openDB } from 'idb';

export interface ReadingListItem {
  isbn: string;
  title: string;
  addedAt: string;
}

export interface PendingReview {
  idempotencyKey: string;
  isbn: string;
  rating: number;
  comment: string;
  createdAt: string;
}

interface BookstoreSchema extends DBSchema {
  readingList: { key: string; value: ReadingListItem; indexes: { byAddedAt: string } };
  outbox: { key: string; value: PendingReview; indexes: { byCreatedAt: string } };
}

@Service()
export class BookstoreDatabase {
  // true when another tab opened a newer schema and this tab gave up its connection
  readonly outdated = signal(false);

  private readonly db = openDB<BookstoreSchema>('bookstore', 2, {
    upgrade(db, oldVersion, _newVersion, tx) {
      if (oldVersion < 1) {
        db.createObjectStore('readingList', { keyPath: 'isbn' });
      }
      if (oldVersion < 2) {
        tx.objectStore('readingList').createIndex('byAddedAt', 'addedAt');
        const outbox = db.createObjectStore('outbox', { keyPath: 'idempotencyKey' });
        outbox.createIndex('byCreatedAt', 'createdAt');
      }
    },
    blocking: (_current, _blocked, event) => {
      (event.target as IDBDatabase).close(); // let the other tab upgrade
      this.outdated.set(true);
    },
  });

  connection(): Promise<IDBPDatabase<BookstoreSchema>> {
    return this.db;
  }
}
