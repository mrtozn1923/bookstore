import { inject } from '@angular/core';
import { CatalogService } from './catalog.service';

export function loadDystopias() {
  return inject(CatalogService).listBooks({ category: 'dystopia', inStock: true, pageSize: 20 });
}
