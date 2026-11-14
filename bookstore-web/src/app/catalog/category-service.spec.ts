import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { apiUrl } from '../api';
import { CategoryService } from './category-service';

describe('CategoryService', () => {
  let service: CategoryService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CategoryService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    // fails the test if a request was sent that the test did not expect
    httpTesting.verify();
  });

  it('sends one request for every subscriber, early or late', () => {
    const seen: string[][] = [];
    service.categories$.subscribe((list) => seen.push(list));
    service.categories$.subscribe((list) => seen.push(list));

    // expectOne fails if there are zero or two matching requests
    const request = httpTesting.expectOne(`${apiUrl}/categories`);
    expect(request.request.method).toBe('GET');
    request.flush(['science-fiction', 'classics']);

    // a late subscriber gets the cached list without a new request
    service.categories$.subscribe((list) => seen.push(list));
    expect(seen).toEqual([
      ['science-fiction', 'classics'],
      ['science-fiction', 'classics'],
      ['science-fiction', 'classics'],
    ]);
  });
});
