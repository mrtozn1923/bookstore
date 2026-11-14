import { HttpClient } from '@angular/common/http';
import { Component, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { apiUrl } from '../api';
import type { Book } from './book';

@Component({
  selector: 'app-book-page',
  imports: [RouterLink],
  template: `
    @if (book.value(); as b) {
      <h1>{{ b.title }}</h1>
      <p>{{ b.author }} - {{ b.price }} TRY - {{ b.stock }} in stock</p>
    } @else if (book.isLoading()) {
      <p>Loading...</p>
    }
    <a routerLink="/">Back to catalog</a>
  `,
})
export class BookPage {
  readonly #http = inject(HttpClient);
  // filled from the :isbn route parameter by withComponentInputBinding()
  readonly isbn = input.required<string>();

  protected readonly book = rxResource({
    params: () => this.isbn(),
    stream: ({ params: isbn }) => this.#http.get<Book>(`${apiUrl}/books/${isbn}`),
  });
}
