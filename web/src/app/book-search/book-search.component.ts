import { Component, inject, signal } from '@angular/core';
import { NgFor } from '@angular/common';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { BookService } from '../book.service';

@Component({
  selector: 'app-book-search',
  standalone: true,
  imports: [NgFor],
  template: `
    <input (input)="query.set($any($event.target).value)" placeholder="Search books...">
    <p *ngFor="let title of results()">{{ title }}</p>
  `
})
export class BookSearchComponent {
  private bookService = inject(BookService);

  query = signal('');

  results = toSignal(
    toObservable(this.query).pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(q => this.bookService.search(q))
    ),
    { initialValue: [] }
  );
}
