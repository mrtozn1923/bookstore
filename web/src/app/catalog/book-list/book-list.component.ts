import { Component } from '@angular/core';
import { Book } from '../../book';

@Component({
  selector: 'app-book-list',
  templateUrl: './book-list.component.html',
  styleUrls: ['./book-list.component.css']
})
export class BookListComponent {
  books: Book[] = [
    { isbn: '9780441172719', title: 'Dune', price: 450 },
    { isbn: '9780141439587', title: 'Emma', price: 320 }
  ];
  cart: Book[] = [];

  onAddToCart(book: Book): void {
    this.cart.push(book);
  }
}
