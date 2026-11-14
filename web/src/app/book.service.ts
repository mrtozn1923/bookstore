import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { Book } from './book';

@Injectable({ providedIn: 'root' })
export class BookService {
  private titles = ['Dune', 'Emma', 'The Hobbit', 'Middlemarch', 'Frankenstein', 'Pride and Prejudice'];

  private books: Book[] = [
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

  search(term: string): Observable<string[]> {
    const query = term.toLowerCase();
    const matches = this.titles.filter(t => t.toLowerCase().includes(query));
    return of(matches).pipe(delay(300));
  }

  getBook(isbn: string): Observable<Book | undefined> {
    return of(this.books.find(book => book.isbn === isbn)).pipe(delay(300));
  }
}
