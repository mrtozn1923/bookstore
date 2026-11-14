import { Component } from '@angular/core';
import { CartService } from '../cart.service';
import { NgIf, NgFor, AsyncPipe } from '@angular/common';

@Component({
    selector: 'app-cart-badge',
    template: `
    <span>Books in cart: {{ count$ | async }}</span>
    <div *ngIf="items$ | async as items">
      <p *ngFor="let item of items">{{ item.title }}</p>
    </div>
  `,
    standalone: true,
    imports: [NgIf, NgFor, AsyncPipe]
})
export class CartBadgeComponent {
  count$ = this.cartService.count$;
  items$ = this.cartService.items$;

  constructor(private cartService: CartService) { }
}
