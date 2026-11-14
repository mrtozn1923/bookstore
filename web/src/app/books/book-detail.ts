import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BookPitch } from './book-pitch';
import { ReviewSummary } from './review-summary';

const BOOKS: Record<string, string> = {
  '9780441172719': 'Dune',
  '9780141439587': 'Emma',
  '9780547928227': 'The Hobbit',
};

@Component({
  selector: 'app-book-detail',
  imports: [BookPitch, ReviewSummary, RouterLink],
  template: `
    <nav>
      @for (book of books; track book.isbn) {
        <a [routerLink]="['/books', book.isbn]">{{ book.title }}</a>
      }
    </nav>
    <h1>{{ title() }}</h1>
    <app-book-pitch [title]="title()" />
    <h2>What readers say</h2>
    <app-review-summary [isbn]="isbn()" />
  `,
  styles: `nav { display: flex; gap: 12px; }`,
})
export class BookDetail {
  // Route parametresi withComponentInputBinding() sayesinde input olarak gelir
  readonly isbn = input.required<string>();
  protected readonly title = computed(() => BOOKS[this.isbn()] ?? 'Unknown book');
  protected readonly books = Object.entries(BOOKS).map(([isbn, title]) => ({ isbn, title }));
}
