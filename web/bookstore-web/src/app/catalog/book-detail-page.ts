import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { BookDetails } from '../core/api-models';
import { CartStore } from '../cart/cart-store';

@Component({
  selector: 'app-book-detail-page',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './book-detail-page.html',
})
export class BookDetailPage {
  private readonly cartStore = inject(CartStore);
  private readonly router = inject(Router);

  // Bound from the route parameter :isbn (withComponentInputBinding).
  readonly isbn = input.required<string>();

  protected readonly book = httpResource<BookDetails>(() => `/api/books/${this.isbn()}`);
  protected readonly added = signal(false);

  protected async addToCart(quantity: number): Promise<void> {
    await this.cartStore.add(this.isbn(), quantity);
    this.added.set(true);
  }

  protected async buyNow(quantity: number): Promise<void> {
    await this.cartStore.add(this.isbn(), quantity);
    await this.router.navigate(['/cart']);
  }
}
