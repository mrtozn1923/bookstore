import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CartSummary } from './cart-summary';

@Component({
  selector: 'app-cart-page',
  imports: [CartSummary, RouterLink],
  template: `
    <h1>Cart</h1>
    <app-cart-summary>
      <a cartActions routerLink="/checkout">Checkout</a>
    </app-cart-summary>
  `,
})
export class CartPage {}
