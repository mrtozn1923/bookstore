import { AsyncPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { catchError, of, switchMap } from 'rxjs';
import { apiUrl } from '../api';
import { settledTerm } from '../core/operators';
import type { Book } from '../catalog/book';

// 1. RxJS only: the form control's valueChanges, the async pipe renders the result
@Component({
  selector: 'app-search-rxjs',
  imports: [ReactiveFormsModule, AsyncPipe],
  template: `
    <input id="search-rxjs" [formControl]="term" placeholder="Search books" />
    <ul>
      @for (book of results$ | async; track book.isbn) {
        <li>{{ book.title }}</li>
      }
    </ul>
  `,
})
export class SearchRxjs {
  readonly #http = inject(HttpClient);
  protected readonly term = new FormControl('', { nonNullable: true });
  protected readonly results$ = this.term.valueChanges.pipe(
    settledTerm(300),
    switchMap((term) =>
      term
        ? this.#http
            .get<Book[]>(`${apiUrl}/books`, { params: { q: term } })
            .pipe(catchError(() => of<Book[]>([])))
        : of<Book[]>([]),
    ),
  );
}
