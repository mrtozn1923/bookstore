import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { CartStore } from './cart/cart-store';
import { CartToast } from './cart/cart-toast';
import { ServerStatus } from './core/server-status';

@Component({
  imports: [RouterOutlet, RouterLink, CartToast],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly cart = inject(CartStore);
  protected readonly serverStatus = inject(ServerStatus);
}
