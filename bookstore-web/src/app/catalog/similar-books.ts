import { Component, computed, input, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Book } from './book';

@Component({
  selector: 'app-similar-books',
  imports: [CurrencyPipe, RouterLink],
  template: `
    <!-- Heading and button share one fixed-height row: the button appearing later moves nothing -->
    <div class="head">
      <h2>Similar books</h2>
      @if (!showAll() && (similar.value()?.length ?? 0) > 4) {
        <button type="button" class="more" (click)="showAll.set(true)">Show more</button>
      }
    </div>
    <ul class="cards">
      @for (book of visible(); track book.isbn) {
        <li>
          <a [routerLink]="['/books', book.isbn]">{{ book.title }}</a>
          <span>{{ book.author }}</span>
          <span class="price">{{ book.price | currency: book.currency }}</span>
        </li>
      } @empty {
        <!-- Skeleton while loading: same cards, same height, nothing moves when the data arrives -->
        @for (i of skeleton; track i) {
          <li class="skeleton" aria-hidden="true"></li>
        }
      }
    </ul>
  `,
  styles: `
    /* padding instead of the heading's margin-top: a child's top margin collapses through the slot */
    :host { display: block; padding-top: 24px; }
    .head { display: flex; align-items: center; justify-content: space-between; height: 40px; margin-bottom: 12px; }
    h2 { margin: 0; font-size: 22px; }
    .cards { list-style: none; padding: 0; margin: 0; display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
    .cards li { display: flex; flex-direction: column; gap: 4px; height: 104px; padding: 12px;
                border: 1px solid #e2e8f0; border-radius: 8px; background: #fff; overflow: hidden; }
    .cards li.skeleton { background: #e2e8f0; }
    .price { font-weight: 700; margin-top: auto; }
    .more { padding: 6px 12px; font: inherit; }
    @media (max-width: 640px) { .cards { grid-template-columns: repeat(2, 1fr); } }
  `,
})
export class SimilarBooks {
  readonly isbn = input.required<string>();
  protected readonly similar = httpResource<Book[]>(() => `/api/books/${this.isbn()}/similar?count=8`);
  // 4 cards at first, the other 4 behind "Show more"
  protected readonly showAll = signal(false);
  protected readonly skeleton = [1, 2, 3, 4];
  protected readonly visible = computed(() => (this.similar.value() ?? []).slice(0, this.showAll() ? 8 : 4));
}
