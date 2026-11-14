import { Component } from '@angular/core';
import { SearchInterop } from './search-interop';
import { SearchResource } from './search-resource';
import { SearchRxjs } from './search-rxjs';

@Component({
  selector: 'app-search-page',
  imports: [SearchRxjs, SearchInterop, SearchResource],
  template: `
    <h1>Search</h1>
    <h2>1. RxJS only</h2>
    <app-search-rxjs />
    <h2>2. toObservable + toSignal</h2>
    <app-search-interop (searched)="onSearched($event)" />
    <h2>3. httpResource + debounced</h2>
    <app-search-resource />
  `,
})
export class SearchPage {
  protected onSearched(term: string): void {
    console.log(`[search] searched for "${term}"`);
  }
}
