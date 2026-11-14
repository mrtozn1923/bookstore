import { Component } from '@angular/core';
import { LowerCasePipe, DecimalPipe, PercentPipe, CurrencyPipe, DatePipe } from '@angular/common';

@Component({
    selector: 'app-order-summary',
    template: `
    <p>{{ orderDate | date }}</p>
    <p>{{ orderDate | date:'dd.MM.yyyy HH:mm' }}</p>
    <p>{{ orderDate | date:'d MMMM y, EEEE' }}</p>
    <p>{{ total | number }}</p>
    <p>{{ rating | number:'1.1-2' }}</p>
    <p>{{ discount | percent }}</p>
    <p>{{ total | currency:'TRY' }}</p>
    <p>{{ total | currency:'TRY':'symbol-narrow' }}</p>
    <p>{{ total | currency }}</p>
    <p>{{ orderDate | date:'MMMM' | lowercase }}</p>
  `,
    standalone: true,
    imports: [LowerCasePipe, DecimalPipe, PercentPipe, CurrencyPipe, DatePipe]
})
export class OrderSummaryComponent {
  orderDate = new Date(2021, 4, 12, 14, 30);
  total = 1540;
  discount = 0.15;
  rating = 4.6666;
}
