import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, inject, input } from '@angular/core';
import { Book } from '../catalog/book';
import { ReadingListStore } from '../reading-list/reading-list-store';
import { ReviewOutbox } from '../reviews/review-outbox';

@Component({
  selector: 'app-book-page',
  imports: [CurrencyPipe],
  template: `
    @if (book.hasValue()) {
      <h1>{{ book.value().title }}</h1>
      <p>{{ book.value().author }}, {{ book.value().price | currency: 'TRY' }}</p>
      <button (click)="readingList.add(book.value())">Add to reading list</button>

      <form
        (submit)="$event.preventDefault(); review(rating.value, comment.value); comment.value = ''"
      >
        <select #rating>
          @for (n of [5, 4, 3, 2, 1]; track n) {
            <option [value]="n">{{ n }} stars</option>
          }
        </select>
        <textarea #comment placeholder="Your review"></textarea>
        <button type="submit">Send review</button>
      </form>
      @if (outbox.pending()) {
        <p class="pending">{{ outbox.pending() }} review(s) waiting for a connection</p>
      }
    } @else if (book.error()) {
      <p>This book could not be loaded.</p>
    }

    <h2>Reading list</h2>
    <ul class="reading-list">
      @for (item of readingList.items.value(); track item.isbn) {
        <li>{{ item.title }}</li>
      }
    </ul>
  `,
})
export class BookPage {
  readonly isbn = input.required<string>();
  protected readonly book = httpResource<Book>(() => `/api/books/${this.isbn()}`);
  protected readonly readingList = inject(ReadingListStore);
  protected readonly outbox = inject(ReviewOutbox);

  review(rating: string, comment: string) {
    void this.outbox.enqueue(this.isbn(), Number(rating), comment);
  }
}
