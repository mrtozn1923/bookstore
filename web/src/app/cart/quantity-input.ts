import { Component, input, model } from '@angular/core';

@Component({
  selector: 'app-quantity-input',
  template: `
    <button type="button" aria-label="Decrease quantity" [disabled]="quantity() <= 1" (click)="quantity.set(quantity() - 1)">
      −
    </button>
    <span class="quantity">{{ quantity() }}</span>
    <button type="button" aria-label="Increase quantity" [disabled]="quantity() >= max()" (click)="quantity.set(quantity() + 1)">
      +
    </button>
  `,
})
export class QuantityInput {
  readonly quantity = model(1);
  readonly max = input(99);
}
