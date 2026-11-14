/// <reference lib="webworker" />
import { expose } from 'comlink';
import { buildIndex, CatalogEntry, CatalogSearchApi, IndexedEntry, search } from './catalog-search';

let index: IndexedEntry[] = [];

const api: CatalogSearchApi = {
  // the worker downloads, parses and indexes the 5,000 books itself: the main thread does none of it
  async load(url: string): Promise<number> {
    const entries: CatalogEntry[] = await (await fetch(url)).json();
    index = buildIndex(entries);
    return index.length;
  },
  search(query) {
    return search(index, query);
  },
};

expose(api);
