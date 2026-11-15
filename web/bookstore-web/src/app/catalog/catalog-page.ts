import { Component, computed, effect, inject, input, RESPONSE_INIT } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { API_BASE } from '../api';
import { normalizeCatalogQuery } from './catalog-query';

interface CatalogPageData {
  items: { slug: string; title: string; author: string }[];
  page: number;
  totalPages: number;
}

@Component({
  selector: 'app-catalog-page',
  imports: [RouterLink],
  template: `
    @if (data.hasValue()) {
      <h1>{{ genre() }}</h1>
      <ul>
        @for (b of data.value().items; track b.slug) {
          <li><a [routerLink]="['/books', b.slug]">{{ b.title }}</a>, {{ b.author }}</li>
        }
      </ul>
      <!-- gerçek <a href>: crawler sayfaları link'ten bulur, butona tıklamaz -->
      <nav>
        @if (data.value().page > 1) {
          <a [routerLink]="[]" [queryParams]="{ page: null }" queryParamsHandling="merge">First page</a>
        }
        @if (data.value().page < data.value().totalPages) {
          <a [routerLink]="[]" [queryParams]="{ page: data.value().page + 1 }" queryParamsHandling="merge">Next</a>
        }
      </nav>
    } @else if (data.error()) {
      <h1>No books found</h1>
    }
  `,
})
export class CatalogPage {
  readonly genre = input.required<string>();
  readonly language = input<string>();
  readonly format = input<string>();
  readonly sort = input<string>();
  readonly page = input<string>();

  private readonly api = inject(API_BASE);
  private readonly responseInit = inject(RESPONSE_INIT, { optional: true });

  private readonly url = computed(() => {
    const q = normalizeCatalogQuery({ format: this.format(), sort: this.sort(), page: this.page() });
    const path = [this.genre(), this.language()].filter(Boolean).join('/');
    return q.valid ? { url: `${this.api}/api/catalog/${path}`, params: q.params } : undefined;
  });

  protected readonly data = httpResource<CatalogPageData>(() => this.url());

  constructor() {
    effect(() => {
      // sonuçsuz ya da geçersiz filtre: SSR yanıtı 200 değil 404 (soft 404 olmasın)
      if ((this.data.error() || !this.url()) && this.responseInit) this.responseInit.status = 404;
    });
  }
}
