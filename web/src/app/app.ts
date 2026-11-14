import { Component } from '@angular/core';
import { BookCard } from './book-card/book-card';
import { BOOKS } from './book';

@Component({
  imports: [BookCard],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly books = BOOKS;
}
