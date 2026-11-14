import { Component, computed, input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-price-tag',
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<span>{{ finalPrice() }} {{ currency() }}</span>`
})
export class PriceTagComponent {
  price = input.required<number>();
  currency = input('TRY');
  discount = input(0);

  finalPrice = computed(() => this.price() * (1 - this.discount() / 100));
}
