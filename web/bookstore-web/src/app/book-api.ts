import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Book } from './book';

@Injectable({ providedIn: 'root' })
export class BookApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/books';

  getAll(): Observable<Book[]> {
    return this.http.get<Book[]>(this.baseUrl);
  }
}
