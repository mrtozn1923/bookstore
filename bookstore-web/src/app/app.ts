import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { CartStore } from './cart/cart-store';
import { CategoryMenu } from './catalog/category-menu';
import { SearchBox } from './search/search-box';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, CategoryMenu, SearchBox],
  template: `
    <header>
      <a routerLink="/">Bookstore</a>
      <app-category-menu />
      <app-search-box />
      <a class="cart-link" routerLink="/cart">Cart ({{ cart.count() }})</a>
    </header>
    <main><router-outlet /></main>
  `,
})
export class App {
  protected readonly cart = inject(CartStore);
}
