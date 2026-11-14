import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import type { components, operations } from './api/bookstore';

// Types generated from contract/bookstore.openapi.yaml (npm run generate:client); never edit them by hand.
export type Book = components['schemas']['Book'];
export type BookList = components['schemas']['BookList'];
export type ListBooksQuery = Omit<NonNullable<operations['listBooks']['parameters']['query']>, 'api-version'>;

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);

  // Parameter names and types come from the contract: a misspelled filter does not compile.
  listBooks(query: ListBooksQuery = {}): Observable<BookList> {
    let params = new HttpParams().set('api-version', '2026-11-15');
    for (const [name, value] of Object.entries(query)) {
      if (value !== undefined) {
        params = params.set(name, String(value));
      }
    }

    return this.http.get<BookList>('/books', { params });
  }
}
