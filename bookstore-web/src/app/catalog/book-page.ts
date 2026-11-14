import { Component, input } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { CurrencyPipe, NgOptimizedImage } from '@angular/common';
import { Book } from './book';
import { SimilarBooks } from './similar-books';

@Component({
  selector: 'app-book-page',
  imports: [CurrencyPipe, NgOptimizedImage, SimilarBooks],
  template: `
    @if (book.value(); as b) {
      <article class="book">
        @if (b.coverUrl) {
          <img [ngSrc]="b.coverUrl" width="1200" height="1800" priority
               sizes="(max-width: 640px) 100vw, 320px" [alt]="b.title" />
        }
        <div>
          <h1>{{ b.title }}</h1>
          <p>{{ b.author }} · {{ b.publisher }} · {{ b.year }}</p>
          <p class="price">{{ b.price | currency: b.currency }}</p>
          <p>{{ b.summary }}</p>
        </div>
      </article>
      <!-- The slot keeps the block's final height: placeholder, skeleton and cards all fit in it -->
      <div class="similar-slot">
        <!-- SSR renders the block into the HTML; its JavaScript loads and hydrates when it becomes visible -->
        @defer (on viewport; hydrate on viewport) {
          <app-similar-books [books]="similar.value()" />
        } @placeholder {
          <div></div>
        }
      </div>
      <section class="details">
        <h2>Details</h2>
        <dl>
          <dt>ISBN</dt><dd>{{ b.isbn }}</dd>
          <dt>Publisher</dt><dd>{{ b.publisher }}</dd>
          <dt>Year</dt><dd>{{ b.year }}</dd>
          <dt>Category</dt><dd>{{ b.category }}</dd>
          <dt>Availability</dt><dd>{{ b.stock > 0 ? b.stock + ' in stock' : 'Sold out' }}</dd>
          <dt>Shipping</dt><dd>Ships in 1-2 business days</dd>
        </dl>
      </section>
    }
  `,
  styles: `
    .book { display: grid; grid-template-columns: minmax(0, 320px) 1fr; gap: 24px; }
    .book img { width: 100%; height: auto; border-radius: 12px; }
    .price { font-size: 24px; font-weight: 700; }
    /* 24 px padding + 40 px heading row + 12 px gap + one row of 104 px cards; two rows on narrow screens */
    .similar-slot { min-height: 180px; }
    .details dl { display: grid; grid-template-columns: 140px 1fr; gap: 8px 16px; }
    .details dt { color: #64748b; }
    .details dd { margin: 0; }
    @media (max-width: 640px) {
      .book { grid-template-columns: 1fr; }
      .similar-slot { min-height: 296px; }
    }
  `,
})
export class BookPage {
  readonly isbn = input.required<string>();
  protected readonly book = httpResource<Book>(() => `/api/books/${this.isbn()}`);
  // Requested together with the book, during the first render: on the server, and in the browser
  // from the HTTP transfer cache
  protected readonly similar = httpResource<Book[]>(() => `/api/books/${this.isbn()}/similar?count=8`);
}
