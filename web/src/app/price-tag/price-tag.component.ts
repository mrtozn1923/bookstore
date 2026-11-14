import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';

@Component({
    selector: 'app-price-tag',
    template: `<p>{{ price }} TRY @if (trend) {<span>{{ trend }}</span>}</p>`,
    standalone: true,
})
export class PriceTagComponent implements OnChanges {
  @Input() price = 0;
  trend = '';

  ngOnChanges(changes: SimpleChanges): void {
    const change = changes['price'];
    if (change && !change.firstChange) {
      this.trend = change.currentValue > change.previousValue ? '▲' : '▼';
    }
  }
}
