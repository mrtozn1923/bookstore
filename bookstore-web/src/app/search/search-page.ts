import { Component, inject, input, resource, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CatalogSearchService } from './catalog-search-service';
import { MainThreadSearch } from './main-thread-search';

@Component({
  selector: 'app-search-page',
  imports: [RouterLink],
  template: `
    <h1>Search</h1>
    <input
      type="search"
      placeholder="Title, author or subject"
      [value]="query()"
      (input)="query.set($any($event.target).value)"
    />
    <p class="status">{{ status.value() ?? 'Loading the catalog...' }}</p>
    <ul>
      @for (hit of results.value() ?? []; track hit.isbn) {
        <li>
          <a [routerLink]="['/books', hit.isbn]">{{ hit.title }}</a>, {{ hit.author }}
        </li>
      }
    </ul>
  `,
})
export class SearchPage {
  private readonly worker = inject(CatalogSearchService);
  private readonly mainThread = inject(MainThreadSearch);
  // ?mode=main or ?mode=yield to compare; the app uses the worker
  readonly mode = input<'worker' | 'main' | 'yield'>('worker');
  protected readonly query = signal('');

  protected readonly status = resource({
    loader: async () => {
      const count =
        this.mode() === 'worker'
          ? await this.worker.ready()
          : (await this.mainThread.ready()).length;
      return `${count} books indexed (${this.mode()})`;
    },
  });

  protected readonly results = resource({
    params: () => this.query(),
    loader: ({ params, abortSignal }) =>
      this.mode() === 'worker'
        ? this.worker.search(params)
        : this.mode() === 'yield'
          ? this.mainThread.searchYielding(params, abortSignal)
          : this.mainThread.search(params),
  });
}
