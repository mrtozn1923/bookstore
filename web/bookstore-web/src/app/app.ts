import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { Cart } from './core/cart';
import { OrderStatusHub } from './core/order-status';
import { Session } from './core/session';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: `
    <header>
      <a routerLink="/" class="brand">Bookstore</a>
      <a routerLink="/cart">Cart ({{ cart.count() }})</a>
      @if (session.user(); as user) {
        <a routerLink="/orders">My orders</a>
        <span class="user" [title]="'Live updates: ' + hub.state()">{{ user.name }}</span>
        <button (click)="session.signOut()">Sign out</button>
      } @else if (!session.isLoading()) {
        <button (click)="session.signIn()">Sign in</button>
      }
    </header>
    <main><router-outlet /></main>
  `
})
export class App {
  protected readonly session = inject(Session);
  protected readonly cart = inject(Cart);
  // Injected here so that the hub connects as soon as the user is signed in, on any page
  protected readonly hub = inject(OrderStatusHub);
}
