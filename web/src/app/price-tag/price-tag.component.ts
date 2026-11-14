import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-price-tag',
  template: `<span>{{ finalPrice() }} {{ currency() }}</span>`
})
export class PriceTagComponent {
  price = input.required<number>();
  currency = input('TRY');
  discount = input(0);

  finalPrice = computed(() => this.price() * (1 - this.discount() / 100));
}
