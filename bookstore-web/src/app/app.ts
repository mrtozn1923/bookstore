import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CartStore } from './cart/cart-store';
import { CartToast } from './cart/cart-toast';

@Component({
  imports: [RouterOutlet, CartToast],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly cart = inject(CartStore);
}
