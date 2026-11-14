import { Component } from '@angular/core';

@Component({
  selector: 'app-wishlist-button',
  standalone: false,
  template: `
    <button (click)="add()">Add to wishlist</button>
    <span class="count">{{ count }}</span>
  `,
})
export class WishlistButtonComponent {
  count = 0;

  add(): void {
    this.count++;
  }
}
