import { Component, input } from '@angular/core';
import { Money, MoneyPipe } from '@bookstore/ui/money';

@Component({
  selector: 'bs-price-tag',
  imports: [MoneyPipe],
  template: `
    <span class="price">{{ price() | money: locale() }}</span>
    @if (listPrice(); as old) {
      <s class="list-price">{{ old | money: locale() }}</s>
    }
  `,
})
export class PriceTag {
  readonly price = input.required<Money>();
  readonly listPrice = input<Money>();
  readonly locale = input('en-US');
}
