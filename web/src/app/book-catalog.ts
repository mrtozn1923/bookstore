import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { RuntimeConfigService } from './runtime-config';

export interface Book {
  isbn: string;
  title: string;
  shelfLabel: string;
  priceTry: number;
}

@Injectable({ providedIn: 'root' })
export class BookCatalogService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(RuntimeConfigService);

  getBooks(): Observable<Book[]> {
    return this.http.get<Book[]>(`${this.config.value.apiBaseUrl}/books`);
  }
}
