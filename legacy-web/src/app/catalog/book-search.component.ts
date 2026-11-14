import { Component, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Observable, debounceTime, of, switchMap } from 'rxjs';
import { Book, BookService } from './book.service';

@Component({
  selector: 'app-book-search',
  standalone: false,
  template: `
    <input type="search" aria-label="Search books" [formControl]="term" />
    <ul>
      <li *ngFor="let book of results$ | async">{{ book.title }}</li>
    </ul>
  `,
})
export class BookSearchComponent implements OnInit {
  term = new FormControl('', { nonNullable: true });
  results$: Observable<Book[]> = of([]);

  constructor(private books: BookService) {}

  ngOnInit(): void {
    this.results$ = this.term.valueChanges.pipe(
      debounceTime(300),
      switchMap((term) => (term.length < 2 ? of([]) : this.books.search(term))),
    );
  }
}
