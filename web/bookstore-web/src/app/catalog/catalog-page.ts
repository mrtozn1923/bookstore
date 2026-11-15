import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BookSummary, CategoryView, PagedResult } from '../core/api-models';
import { CartStore } from '../cart/cart-store';
import { PageHeading } from '../core/page-heading';

@Component({
  selector: 'app-catalog-page',
  imports: [CurrencyPipe, RouterLink, PageHeading],
  templateUrl: './catalog-page.html',
})
export class CatalogPage {
  protected readonly cartStore = inject(CartStore);
  protected readonly search = signal('');
  protected readonly category = signal('');
  protected readonly filtersOpen = signal(true);

  protected readonly categories = httpResource<CategoryView[]>(() => '/api/categories', { defaultValue: [] });

  // Re-runs automatically whenever search or category changes.
  protected readonly books = httpResource<PagedResult<BookSummary>>(() => ({
    url: '/api/books',
    params: { search: this.search(), category: this.category(), pageSize: 50 },
  }));

  protected applySearch(event: Event, value: string): void {
    event.preventDefault();
    this.search.set(value.trim());
  }
}
