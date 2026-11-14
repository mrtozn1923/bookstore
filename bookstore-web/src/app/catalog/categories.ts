import { httpResource } from '@angular/common/http';
import { Service } from '@angular/core';
import { apiUrl } from '../api';
import type { Category } from '../models';

// Server state shared by every component that shows categories: one request, one copy
@Service()
export class Categories {
  readonly #list = httpResource<Category[]>(() => `${apiUrl}/categories`, { defaultValue: [] });
  readonly list = this.#list.asReadonly();
}
