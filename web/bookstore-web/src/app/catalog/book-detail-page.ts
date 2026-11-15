import { Tab, TabContent, TabList, TabPanel, Tabs } from '@angular/aria/tabs';
import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, effect, inject, input, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { BookDetails } from '../core/api-models';
import { CartStore } from '../cart/cart-store';
import { Announcer } from '../core/announcer';
import { PageHeading } from '../core/page-heading';

@Component({
  selector: 'app-book-detail-page',
  imports: [CurrencyPipe, RouterLink, PageHeading, Tabs, TabList, Tab, TabPanel, TabContent],
  templateUrl: './book-detail-page.html',
})
export class BookDetailPage {
  private readonly cartStore = inject(CartStore);
  private readonly announcer = inject(Announcer);

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

  protected async addToCart(book: BookDetails, quantity: number): Promise<void> {
    await this.cartStore.add(book.isbn, quantity);
    this.added.set(true);
    // The visible "Added." stays the same on the second click; the announcement names the book and the new count
    const count = this.cartStore.itemCount();
    this.announcer.announce(`${book.title} added to your cart. Cart: ${count} ${count === 1 ? 'item' : 'items'}.`);
  }
}
