import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Categories } from './categories';

@Component({
  selector: 'app-category-menu',
  imports: [RouterLink],
  template: `
    <nav class="category-menu">
      @for (c of categories.list.value(); track c.slug) {
        <a routerLink="/" [queryParams]="{ category: c.slug }">{{ c.name }}</a>
      }
    </nav>
  `,
})
export class CategoryMenu {
  protected readonly categories = inject(Categories);
}
