import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CartStore } from './cart/cart-store';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  templateUrl: './app.html',
})
export class App implements OnInit {
  private readonly cartStore = inject(CartStore);

  ngOnInit(): void {
    void this.cartStore.load();
  }
}
