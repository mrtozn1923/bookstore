import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BookSales } from './sales-chart/book-sales';
import { SalesChartComponent } from './sales-chart/sales-chart.component';

@Component({
    selector: 'app-admin',
    templateUrl: './admin.component.html',
    styleUrls: ['./admin.component.css'],
    standalone: true,
    imports: [RouterLink, SalesChartComponent]
})
export class AdminComponent {
  sales: BookSales[] = [
    { title: 'Dune', copies: 42 },
    { title: 'Emma', copies: 31 },
    { title: 'The Hobbit', copies: 27 },
    { title: 'Middlemarch', copies: 12 }
  ];
}
