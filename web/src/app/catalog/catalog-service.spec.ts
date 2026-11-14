import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Book } from './book';
import { CatalogService } from './catalog-service';

const dune: Book = { id: 1, isbn: '9780441013593', title: 'Dune', author: 'Frank Herbert', category: 'sci-fi', price: 450, stock: 3 };
const emma: Book = { id: 2, isbn: '9780141439587', title: 'Emma', author: 'Jane Austen', category: 'classics', price: 320, stock: 0 };

describe('CatalogService', () => {
  let catalog: CatalogService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    catalog = TestBed.inject(CatalogService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the whole catalog when no filter is set', async () => {
    TestBed.tick();
    const request = http.expectOne('/api/books');
    expect(request.request.method).toBe('GET');
    expect(catalog.books.status()).toBe('loading');

    request.flush([dune, emma]);
    await TestBed.inject(ApplicationRef).whenStable();

    expect(catalog.books.status()).toBe('resolved');
    expect(catalog.books.value().map((book) => book.title)).toEqual(['Dune', 'Emma']);
  });

  it('sends the selected category as a query parameter', () => {
    catalog.category.set('classics');
    TestBed.tick();

    const request = http.expectOne((req) => req.url === '/api/books');
    expect(request.request.params.get('category')).toBe('classics');
  });

  it('cancels the running request when the category changes', () => {
    TestBed.tick();
    const first = http.expectOne('/api/books');

    catalog.category.set('classics');
    TestBed.tick();

    expect(first.cancelled).toBe(true);
    http.expectOne('/api/books?category=classics');
  });

  it('lists only the books that are in stock', async () => {
    TestBed.tick();
    http.expectOne('/api/books').flush([dune, emma]);
    await TestBed.inject(ApplicationRef).whenStable();

    expect(catalog.inStock().map((book) => book.title)).toEqual(['Dune']);
  });

  it('moves to the error state when the API fails', async () => {
    TestBed.tick();
    http.expectOne('/api/books').flush({ title: 'Database unavailable' }, { status: 503, statusText: 'Service Unavailable' });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(catalog.books.status()).toBe('error');
    expect(catalog.books.error()).toBeInstanceOf(HttpErrorResponse);
    expect((catalog.books.error() as HttpErrorResponse).status).toBe(503);
    expect(() => catalog.books.value()).toThrow(/error state/);
  });
});

describe('CatalogService search', () => {
  let catalog: CatalogService;
  let http: HttpTestingController;

  beforeEach(async () => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    catalog = TestBed.inject(CatalogService);
    http = TestBed.inject(HttpTestingController);
    TestBed.tick();
    http.expectOne('/api/books').flush([dune, emma]);
    await vi.advanceTimersByTimeAsync(0);
  });

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  it('waits 300 ms after the last keystroke before searching', async () => {
    catalog.search.set('du');
    await vi.advanceTimersByTimeAsync(200);
    catalog.search.set('dune ');
    await vi.advanceTimersByTimeAsync(299);
    TestBed.tick();
    http.expectNone((req) => req.url === '/api/books');

    await vi.advanceTimersByTimeAsync(1);
    TestBed.tick();

    http.expectOne('/api/books?search=dune').flush([dune]);
    await vi.advanceTimersByTimeAsync(0);
    expect(catalog.books.value()).toEqual([dune]);
  });
});
