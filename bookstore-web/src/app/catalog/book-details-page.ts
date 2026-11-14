import { HttpClient, httpResource } from '@angular/common/http';
import { Component, computed, effect, inject, input } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { patchState, signalState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { apiUrl } from '../api';
import type { Book, BookPage, Review } from '../models';

@Component({
  selector: 'app-book-details-page',
  imports: [RouterLink, RouterOutlet],
  template: `
    @if (book.hasValue()) {
      <h1>{{ book.value().title }}</h1>
      <p class="book">{{ book.value().author }} - {{ book.value().price }} TRY</p>
      <a class="share-link" routerLink="share">Share</a>
      <router-outlet />
    } @else if (book.error()) {
      <p class="book-error">Could not load this book.</p>
    } @else {
      <p>Loading...</p>
    }

    <h2>Reviews</h2>
    <ul class="reviews">
      @for (r of reviews.value(); track r.id) {
        <li>{{ r.author }} ({{ r.rating }}/5): {{ r.text }}</li>
      }
    </ul>
    <p>
      <input class="review-text" [value]="form.text()" (input)="setText($any($event.target).value)" />
      <button class="send" [disabled]="form.saving()" (click)="addReview()">Add review</button>
    </p>
    @if (form.error(); as message) {
      <p class="save-error">{{ message }}</p>
    }

    <h2>More in this category</h2>
    <ul class="related">
      @for (b of relatedBooks(); track b.isbn) {
        <li><a [routerLink]="['/books', b.isbn]">{{ b.title }}</a></li>
      }
    </ul>
  `,
})
export class BookDetailsPage {
  readonly #http = inject(HttpClient);
  // bound from the :isbn route parameter by withComponentInputBinding()
  readonly isbn = input.required<string>();

  protected readonly book = httpResource<Book>(() => `${apiUrl}/books/${this.isbn()}`);
  protected readonly reviews = httpResource<Review[]>(() => `${apiUrl}/books/${this.isbn()}/reviews`, {
    defaultValue: [],
  });
  // Depends on the book's category: waits for the book, follows its loading and error status
  protected readonly related = httpResource<BookPage>(({ chain }) => {
    const category = chain(this.book)?.category;
    return category ? { url: `${apiUrl}/books`, params: { category } } : undefined;
  });
  protected readonly relatedBooks = computed(() =>
    this.related.hasValue() ? this.related.value().items.filter((b) => b.isbn !== this.isbn()) : [],
  );

  // Small local state: one signalState instead of three separate signals
  protected readonly form = signalState({ text: '', saving: false, error: null as string | null });

  constructor() {
    effect(() => console.log(`[book] ${this.book.status()} | [related] ${this.related.status()}`));
    effect(() => console.log(`[reviews] ${this.reviews.status()}: ${this.reviews.value().length} reviews`));
  }

  protected setText(text: string): void {
    patchState(this.form, { text });
  }

  async addReview(): Promise<void> {
    const draft = { author: 'Ada', rating: 5, text: this.form.text() };
    const before = this.reviews.value();
    patchState(this.form, { saving: true, error: null });
    // Optimistic update: show the review now; the resource's status becomes 'local'
    this.reviews.update((list) => [...list, { ...draft, id: -1, isbn: this.isbn() }]);
    try {
      await firstValueFrom(this.#http.post<Review>(`${apiUrl}/books/${this.isbn()}/reviews`, draft));
      patchState(this.form, { text: '' });
      // The server is the source of truth again: fetch the list with the real id
      this.reviews.reload();
    } catch {
      // Roll back to the list we had before the optimistic update
      this.reviews.set(before);
      patchState(this.form, { error: 'Could not save your review. Please try again.' });
    } finally {
      patchState(this.form, { saving: false });
    }
  }
}
