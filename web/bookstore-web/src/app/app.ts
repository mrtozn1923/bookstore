import { Component, signal } from '@angular/core';
import { Book } from './book';
import { BookCard } from './book-card/book-card';

@Component({
  imports: [BookCard],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly books = signal<Book[]>([
    { isbn: '9780441172719', title: 'Dune', author: 'Frank Herbert', price: 349.9 },
    { isbn: '9780547928227', title: 'The Hobbit', author: 'J. R. R. Tolkien', price: 289.5 },
  ]);
}
