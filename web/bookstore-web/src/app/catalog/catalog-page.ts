import { Combobox, ComboboxPopup, ComboboxWidget } from '@angular/aria/combobox';
import { Listbox, Option } from '@angular/aria/listbox';
import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, effect, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BookSummary, CategoryView, PagedResult } from '../core/api-models';
import { CartStore } from '../cart/cart-store';
import { PageHeading } from '../core/page-heading';

@Component({
  selector: 'app-catalog-page',
  imports: [CurrencyPipe, RouterLink, PageHeading, Combobox, ComboboxPopup, ComboboxWidget, Listbox, Option],
  templateUrl: './catalog-page.html',
})
export class CatalogPage {
  protected readonly cartStore = inject(CartStore);
  protected readonly search = signal('');
  protected readonly category = signal('');
  protected readonly filtersOpen = signal(true);
  protected readonly query = signal('');
  protected readonly picked = signal<string[]>([]);
  private readonly combobox = viewChild.required(Combobox);

  protected readonly categories = httpResource<CategoryView[]>(() => '/api/categories', { defaultValue: [] });

  // Every title once, for the suggestions under the search box
  private readonly titles = httpResource<PagedResult<BookSummary>>(() => ({ url: '/api/books', params: { pageSize: 50 } }));
  protected readonly hasQuery = computed(() => this.query().trim().length >= 2);
  protected readonly suggestions = computed(() => {
    const text = this.query().trim();
    return (this.titles.value()?.items ?? []).map(book => book.title)
      .filter(title => title !== text && title.toLowerCase().includes(text.toLowerCase())).slice(0, 8);
  });

  // Re-runs automatically whenever search or category changes.
  protected readonly books = httpResource<PagedResult<BookSummary>>(() => ({
    url: '/api/books',
    params: { search: this.search(), category: this.category(), pageSize: 50 },
  }));

  // The text of the status message: what the list shows now, including the filters that produced it
  protected readonly resultText = computed(() => {
    if (this.books.error()) {
      return 'The catalog could not be loaded. Is Bookstore.Api running?';
    }
    const result = this.books.value();
    if (!result) {
      return '';
    }
    const category = this.categories.value().find(c => c.slug === this.category())?.name;
    return `${result.total} ${result.total === 1 ? 'book' : 'books'}` +
      (category ? ` in ${category}` : '') + (this.search() ? ` for "${this.search()}"` : '');
  });

  constructor() {
    // ngCombobox opens the popup on every keystroke; keep it closed until there are two characters to work with
    effect(() => {
      if (this.combobox().expanded() && !this.hasQuery()) {
        this.combobox().expanded.set(false);
      }
    });
  }

  protected applySearch(event: Event): void {
    event.preventDefault();
    this.combobox().expanded.set(false);
    this.search.set(this.query().trim());
  }

  // Enter or a click on an option: its value is the text to search for (the typed text or a title)
  protected pickSuggestion(): void {
    const text = this.picked()[0];
    if (text) {
      this.query.set(text);
      this.search.set(text);
    }
    this.picked.set([]);
    this.combobox().expanded.set(false);
    // After a click the popup and its options are gone; give focus back to the input
    this.combobox().element.focus();
  }
}
