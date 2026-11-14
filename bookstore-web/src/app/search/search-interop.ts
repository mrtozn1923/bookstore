import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { outputFromObservable, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap } from 'rxjs';
import { apiUrl } from '../api';
import { settledTerm } from '../core/operators';
import type { Book } from '../catalog/book';

// 2. signal in, signal out: RxJS only for the part that is about time
@Component({
  selector: 'app-search-interop',
  template: `
    <input id="search-interop" #box [value]="term()" (input)="term.set(box.value)" />
    <ul>
      @for (book of results(); track book.isbn) {
        <li>{{ book.title }}</li>
      }
    </ul>
  `,
})
export class SearchInterop {
  readonly #http = inject(HttpClient);
  protected readonly term = signal('');

  readonly #settledTerm$ = toObservable(this.term).pipe(settledTerm(300));

  protected readonly results = toSignal(
    this.#settledTerm$.pipe(
      switchMap((term) =>
        term
          ? this.#http
              .get<Book[]>(`${apiUrl}/books`, { params: { q: term } })
              .pipe(catchError(() => of<Book[]>([])))
          : of<Book[]>([]),
      ),
    ),
    { initialValue: [] },
  );

  // the parent can listen with (searched)="…", like any output()
  readonly searched = outputFromObservable(this.#settledTerm$);
}
