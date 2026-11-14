import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { Book, BookService } from './book.service';

describe('BookService', () => {
  let service: BookService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(BookService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should search books with a GET request', async () => {
    const books: Book[] = [{ isbn: '9780441013593', title: 'Dune', price: 450 }];
    const result = firstValueFrom(service.search('dune'));

    const req = httpMock.expectOne('/api/books?search=dune');
    expect(req.request.method).toBe('GET');
    req.flush(books);

    expect(await result).toEqual(books);
  });

  it('should report a server error', async () => {
    const result = firstValueFrom(service.search('dune'));

    httpMock.expectOne('/api/books?search=dune').flush('Server error', { status: 500, statusText: 'Server Error' });

    await expect(result).rejects.toBeInstanceOf(HttpErrorResponse);
    await expect(result).rejects.toHaveProperty('status', 500);
  });
});
