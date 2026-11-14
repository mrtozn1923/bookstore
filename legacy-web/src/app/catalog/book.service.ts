import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Book {
  isbn: string;
  title: string;
  price: number;
}

@Injectable({ providedIn: 'root' })
export class BookService {
  constructor(private http: HttpClient) {}

  search(term: string): Observable<Book[]> {
    return this.http.get<Book[]>('/api/books', { params: { search: term } });
  }
}
