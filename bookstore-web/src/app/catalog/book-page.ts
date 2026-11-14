import { Component, input } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { CurrencyPipe } from '@angular/common';
import { Book } from './book';

@Component({
  selector: 'app-book-page',
  imports: [CurrencyPipe],
  template: `
    @if (book.value(); as b) {
      <article class="book">
        @if (b.coverUrl) {
          <img [src]="b.coverUrl" [alt]="b.title" />
        }
        <div>
          <h1>{{ b.title }}</h1>
          <p>{{ b.author }} · {{ b.publisher }} · {{ b.year }}</p>
          <p class="price">{{ b.price | currency: b.currency }}</p>
          <p>{{ b.summary }}</p>
        </div>
      </article>
    }
  `,
  styles: `
    .book { display: grid; grid-template-columns: minmax(0, 320px) 1fr; gap: 24px; }
    .book img { width: 100%; height: auto; border-radius: 12px; }
    .price { font-size: 24px; font-weight: 700; }
    @media (max-width: 640px) { .book { grid-template-columns: 1fr; } }
  `,
})
export class BookPage {
  readonly isbn = input.required<string>();
  protected readonly book = httpResource<Book>(() => `/api/books/${this.isbn()}`);
}
