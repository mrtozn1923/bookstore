import { Component } from '@angular/core';
import { CartService } from '../cart.service';

@Component({
  selector: 'app-cart-badge',
  template: `<span>Books in cart: {{ cartService.count }}</span>`
})
export class CartBadgeComponent {
  constructor(public cartService: CartService) { }
}
