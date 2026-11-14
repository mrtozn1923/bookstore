import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Book } from '../book';
import { BookEditorComponent } from './book-editor/book-editor.component';
import { BookSales } from './sales-chart/book-sales';
import { SalesChartComponent } from './sales-chart/sales-chart.component';

@Component({
    selector: 'app-admin',
    templateUrl: './admin.component.html',
    styleUrls: ['./admin.component.css'],
    imports: [RouterLink, SalesChartComponent, BookEditorComponent]
})
export class AdminComponent {
  featured: Book = {
    isbn: '9780441172719', title: 'Dune', price: 450, stock: 12,
    description: 'A desert planet, a noble family and a war over the spice melange.'
  };
  editing = false;
  sales: BookSales[] = [
    { title: 'Dune', copies: 42 },
    { title: 'Emma', copies: 31 },
    { title: 'The Hobbit', copies: 27 },
    { title: 'Middlemarch', copies: 12 }
  ];

  onSaved(book: Book): void {
    this.featured = book;
    this.editing = false;
  }
}
