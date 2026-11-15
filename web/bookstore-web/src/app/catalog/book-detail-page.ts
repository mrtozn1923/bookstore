import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, effect, inject, input, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { BookDetails } from '../core/api-models';
import { CartStore } from '../cart/cart-store';
import { PageHeading } from '../core/page-heading';

@Component({
  selector: 'app-book-detail-page',
  imports: [CurrencyPipe, RouterLink, PageHeading],
  templateUrl: './book-detail-page.html',
})
export class BookDetailPage {
  private readonly cartStore = inject(CartStore);

  // Bound from the route parameter :isbn (withComponentInputBinding).
  readonly isbn = input.required<string>();

  protected readonly book = httpResource<BookDetails>(() => `/api/books/${this.isbn()}`);
  protected readonly added = signal(false);

  constructor() {
    // The route title is only "Book · Bookstore"; name the book once it has loaded (WCAG 2.4.2)
    const title = inject(Title);
    effect(() => {
      const book = this.book.value();
      if (book) {
        title.setTitle(`${book.title} · Bookstore`);
      }
    });
  }

  protected async addToCart(quantity: number): Promise<void> {
    await this.cartStore.add(this.isbn(), quantity);
    this.added.set(true);
  }
}
