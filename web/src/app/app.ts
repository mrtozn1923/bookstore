import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import logoUrl from './logo.svg';
import { CartStore } from './shared/cart-store';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly cart = inject(CartStore);
  protected readonly logoUrl = logoUrl;
  protected readonly version = BOOKSTORE_VERSION;
}
