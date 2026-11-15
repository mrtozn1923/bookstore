import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CartStore } from './cart/cart-store';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="site-header">
      <a class="brand" routerLink="/">Bookstore</a>
      <nav aria-label="Main">
        <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Catalog</a>
        <a routerLink="/cart" routerLinkActive="active">Cart ({{ cart.count() }})</a>
      </nav>
    </header>
    <main>
      <router-outlet />
    </main>
    @if (cart.lastAdded(); as title) {
      <p class="toast" role="status">{{ title }} added to cart</p>
    }
  `,
})
export class App {
  protected readonly cart = inject(CartStore);
}
