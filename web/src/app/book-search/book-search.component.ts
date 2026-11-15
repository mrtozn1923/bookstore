import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { BookService } from '../book.service';

@Component({
  selector: 'app-book-search',
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <input (input)="query.set($any($event.target).value)" placeholder="Search books..." i18n-placeholder="@@catalog.searchPlaceholder">
    @for (title of results(); track title) {
      <p>{{ title }}</p>
    }
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
