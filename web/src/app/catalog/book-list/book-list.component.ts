import { Component } from '@angular/core';
import { Book } from '../../book';

@Component({
  selector: 'app-book-list',
  templateUrl: './book-list.component.html',
  styleUrls: ['./book-list.component.css']
})
export class BookListComponent {
  books: Book[] = [
    {
      isbn: '9780441172719', title: 'Dune', price: 450, stock: 12,
      description: 'A desert planet, a noble family and a war over the spice melange.'
    },
    {
      isbn: '9780141439587', title: 'Emma', price: 320, stock: 8,
      description: 'A young matchmaker in an English village keeps getting love wrong.'
    },
    {
      isbn: '9780141439471', title: 'Frankenstein', price: 280, stock: 0,
      description: 'A young scientist gives life to a creature and then runs from it.'
    }
  ];
  cart: Book[] = [];

  onAddToCart(book: Book): void {
    this.cart.push(book);
  }

  addBook(): void {
    const newBook: Book = {
      isbn: '9780547928227', title: 'The Hobbit', price: 390, stock: 5,
      description: 'A comfortable hobbit is pulled into a long journey with thirteen dwarves.'
    };
    this.books = [...this.books, newBook];
  }
}
