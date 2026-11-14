import { Component, Input } from '@angular/core';
import { BookSales } from './book-sales';

@Component({
    selector: 'app-sales-chart',
    imports: [],
    templateUrl: './sales-chart.component.html',
    styleUrl: './sales-chart.component.css'
})
export class SalesChartComponent {
  @Input({ required: true }) data: BookSales[] = [];

  get max(): number {
    return Math.max(...this.data.map(row => row.copies));
  }
}
