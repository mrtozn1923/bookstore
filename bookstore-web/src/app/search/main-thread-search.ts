import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  buildIndex,
  CatalogEntry,
  IndexedEntry,
  rank,
  search,
  SearchHit,
  searchSlice,
  terms,
} from './catalog-search';

// For comparison only: the same search on the main thread, in one piece or in slices
@Service()
export class MainThreadSearch {
  private readonly http = inject(HttpClient);
  private index?: Promise<IndexedEntry[]>;

  ready(): Promise<IndexedEntry[]> {
    this.index ??= firstValueFrom(this.http.get<CatalogEntry[]>('/api/catalog/index')).then(
      buildIndex,
    );
    return this.index;
  }

  async search(query: string): Promise<SearchHit[]> {
    return search(await this.ready(), query);
  }

  // 500 books per slice, giving the main thread back in between; a newer query aborts this one
  async searchYielding(query: string, signal?: AbortSignal): Promise<SearchHit[]> {
    const index = await this.ready();
    const queryTerms = terms(query);
    if (!queryTerms.length) return [];
    const hits: SearchHit[] = [];
    for (let i = 0; i < index.length; i += 500) {
      hits.push(...searchSlice(index.slice(i, i + 500), queryTerms));
      await yieldToMain();
      signal?.throwIfAborted();
    }
    return rank(hits);
  }
}

function yieldToMain(): Promise<void> {
  // lib.dom.d.ts declares scheduler everywhere, but only Chromium 129+ and Firefox 142+ have it
  if ('scheduler' in globalThis && 'yield' in scheduler) return scheduler.yield();
  return new Promise((resolve) => setTimeout(resolve)); // Safari: a macrotask
}
