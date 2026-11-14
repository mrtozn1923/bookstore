import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { Book } from '../models';
import { SearchStore } from './search-store';

const dune = { isbn: '9780441172719', title: 'Dune' } as Book;
const page = (items: Book[]) => ({ items, total: items.length, page: 1, pageSize: 2 });

describe('SearchStore', () => {
  let store: InstanceType<typeof SearchStore>;
  let http: HttpTestingController;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({ providers: [SearchStore, provideHttpClient(), provideHttpClientTesting()] });
    store = TestBed.inject(SearchStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => vi.useRealTimers());

  // the query signal reaches rxMethod through an effect (TestBed.tick), then debounceTime waits 300 ms
  function type(query: string): void {
    store.setQuery(query);
    TestBed.tick();
    vi.advanceTimersByTime(300);
  }

  it('waits for a pause and cancels the request that is no longer needed', () => {
    type('du');
    const first = http.expectOne((r) => r.params.get('q') === 'du');
    type('dun');
    const second = http.expectOne((r) => r.params.get('q') === 'dun');
    expect(first.cancelled).toBe(true);
    second.flush(page([dune]));
    expect(store.results()).toEqual([dune]);
    expect(store.highlightedItem()).toEqual(dune);
  });

  it('keeps searching after a failed request', () => {
    type('em');
    http.expectOne((r) => r.params.get('q') === 'em').flush(null, { status: 503, statusText: 'Service Unavailable' });
    expect(store.requestError()).toBe('HTTP 503');
    type('dune');
    http.expectOne((r) => r.params.get('q') === 'dune').flush(page([dune]));
    expect(store.requestStatus()).toBe('fulfilled');
  });
});
