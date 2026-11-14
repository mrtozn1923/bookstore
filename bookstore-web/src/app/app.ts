import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { CartStore } from './cart/cart-store';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: `
    <header>
      <a routerLink="/">Bookstore</a>
      <a class="cart-link" routerLink="/cart">Cart ({{ cart.count() }})</a>
    </header>
    <main><router-outlet /></main>
  `,
})
export class App {
  protected readonly cart = inject(CartStore);
}
