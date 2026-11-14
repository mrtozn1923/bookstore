import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import type { Book } from '../models';
import { SearchBox } from './search-box';
import { SearchStore } from './search-store';

describe('SearchBox', () => {
  it('renders the store state and sends typing to the store', async () => {
    // a fake store: the component test checks the component, not the store
    const fake = {
      query: signal('em'),
      results: signal([{ isbn: '1', title: 'Emma' }, { isbn: '2', title: 'Middlemarch' }] as Book[]),
      highlighted: signal(1),
      highlightedItem: signal(null),
      isPending: signal(false),
      requestError: signal<string | null>(null),
      setQuery: vi.fn(),
      move: vi.fn(),
    };
    TestBed.configureTestingModule({ imports: [SearchBox], providers: [provideRouter([])] });
    // the store comes from the component's own providers, so it is replaced there
    TestBed.overrideComponent(SearchBox, { set: { providers: [{ provide: SearchStore, useValue: fake }] } });
    const fixture = TestBed.createComponent(SearchBox);
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('li.highlighted')?.textContent?.trim()).toBe('Middlemarch');

    const input = element.querySelector('input')!;
    input.value = 'emm';
    input.dispatchEvent(new Event('input'));
    expect(fake.setQuery).toHaveBeenCalledWith('emm');
  });
});
