import { Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink, RouterOutlet } from '@angular/router';
import { Events } from '@ngrx/signals/events';
import { CartStore } from './cart/cart-store';
import { CategoryMenu } from './catalog/category-menu';
import { SearchBox } from './search/search-box';
import { NotificationsStore } from './shared/notifications-store';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, CategoryMenu, SearchBox],
  template: `
    <header>
      <a routerLink="/">Bookstore</a>
      <app-category-menu />
      <app-search-box />
      <a class="cart-link" routerLink="/cart">Cart ({{ cart.count() }})</a>
      <a routerLink="/orders">Orders</a>
    </header>
    @if (notifications.message(); as message) {
      <p class="toast">{{ message }}</p>
    }
    <main><router-outlet /></main>
  `,
})
export class App {
  protected readonly cart = inject(CartStore);
  protected readonly notifications = inject(NotificationsStore);

  constructor() {
    // Development aid: every dispatched event in the console
    inject(Events)
      .on()
      .pipe(takeUntilDestroyed())
      .subscribe((event) => console.log(`[event] ${event.type}`));
  }
}
