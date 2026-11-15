import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BookApi } from './book-api';

describe('BookApi', () => {
  it('calls GET /api/books', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const api = TestBed.inject(BookApi);
    const http = TestBed.inject(HttpTestingController);

    api.getAll().subscribe();

    http.expectOne('/api/books').flush([]);
    http.verify();
  });
});
