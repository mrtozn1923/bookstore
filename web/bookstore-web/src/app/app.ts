import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { CartStore } from './cart/cart-store';

@Component({
  imports: [RouterOutlet, RouterLink],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App implements OnInit {
  protected readonly cartStore = inject(CartStore);

  ngOnInit(): void {
    void this.cartStore.load();
  }
}
