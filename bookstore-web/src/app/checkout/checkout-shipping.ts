import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CheckoutStore } from './checkout-store';

@Component({
  selector: 'app-checkout-shipping',
  imports: [RouterLink],
  template: `
    <h1>Checkout: shipping</h1>
    <label>
      <input type="radio" name="shipping" value="standard"
        [checked]="checkout.shipping() === 'standard'" (change)="checkout.chooseShipping('standard')" />
      Standard (free over 500 TRY)
    </label>
    <label>
      <input type="radio" name="shipping" value="express"
        [checked]="checkout.shipping() === 'express'" (change)="checkout.chooseShipping('express')" />
      Express (90 TRY)
    </label>
    <a class="next" routerLink="../review">Review order</a>
  `,
})
export class CheckoutShipping {
  protected readonly checkout = inject(CheckoutStore);
}
