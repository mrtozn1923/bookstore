import { Service } from '@angular/core';
import { Remote, wrap } from 'comlink';
import { CatalogSearchApi, SearchHit } from './catalog-search';

@Service()
export class CatalogSearchService {
  private api?: Remote<CatalogSearchApi>;
  private loaded?: Promise<number>;

  // one worker for the whole app, created on first use and never per component
  private start() {
    if (!this.api) {
      const worker = new Worker(new URL('./catalog-search.worker', import.meta.url), {
        type: 'module',
      });
      this.api = wrap<CatalogSearchApi>(worker);
      this.loaded = this.api.load(new URL('/api/catalog/index', location.origin).href);
    }
    return { api: this.api, loaded: this.loaded! };
  }

  ready(): Promise<number> {
    return this.start().loaded;
  }

  async search(query: string): Promise<SearchHit[]> {
    const { api, loaded } = this.start();
    await loaded;
    return api.search(query);
  }
}
