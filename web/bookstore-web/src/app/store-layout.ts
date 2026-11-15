import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { CartStore } from './cart/cart-store';

// Storefront layout (catalog, book, cart); the back office has its own shell under /admin.
@Component({
  selector: 'app-store-layout',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './store-layout.html',
  styleUrl: './store-layout.css',
})
export class StoreLayout {
  protected readonly cartStore = inject(CartStore);
}
