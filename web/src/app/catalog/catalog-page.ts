import { Component, computed, inject, signal } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { CartStore } from '../cart/cart-store';
import { InViewport } from '../shared/in-viewport';
import { PricePipe } from '../shared/price-pipe';
import { BookCard } from './book-card';
import { CatalogService } from './catalog-service';

@Component({
  selector: 'app-catalog-page',
  imports: [BookCard, InViewport, MatFormFieldModule, MatSelectModule, PricePipe],
  template: `
    <aside class="cart-bar" aria-label="Cart status">Cart: {{ cart.count() }} books · {{ cart.total() | price }}</aside>
    <h1>Catalog</h1>
    <input
      type="search"
      aria-label="Search books"
      [value]="catalog.search()"
      (input)="catalog.search.set($any($event.target).value)"
    />
    <mat-form-field>
      <mat-label>Sort by</mat-label>
      <mat-select [value]="sort()" (valueChange)="sort.set($event)">
        <mat-option value="relevance">Relevance</mat-option>
        <mat-option value="title">Title</mat-option>
        <mat-option value="price">Price: low to high</mat-option>
      </mat-select>
    </mat-form-field>
    @switch (catalog.books.status()) {
      @case ('error') {
        <p role="alert">Could not load the catalog.</p>
        <button type="button" (click)="catalog.books.reload()">Try again</button>
      }
      @case ('loading') {
        <p>Loading books…</p>
      }
      @default {
        <ul aria-label="Books">
          @for (book of visibleBooks(); track book.isbn) {
            <li><app-book-card [book]="book" (addToCart)="cart.add($event)" /></li>
          } @empty {
            <li>No books found.</li>
          }
        </ul>
        @if (visibleCount() < books().length) {
          <div class="sentinel" [appInViewport]="visibleCount()" (enter)="showMore()"></div>
        }
      }
    }
  `,
  styles: `
    .cart-bar { position: sticky; top: 0; z-index: 1; padding: 8px 16px; background: #fff; border-bottom: 1px solid #ddd; }
    ul { list-style: none; padding: 0; display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 16px; }
    li { min-height: 180px; }
    .sentinel { height: 1px; }
  `,
})
export class CatalogPage {
  protected readonly catalog = inject(CatalogService);
  protected readonly cart = inject(CartStore);
  protected readonly sort = signal<'relevance' | 'title' | 'price'>('relevance');

  protected readonly books = computed(() => {
    const books = [...this.catalog.inStock()];
    switch (this.sort()) {
      case 'title':
        return books.sort((a, b) => a.title.localeCompare(b.title, 'en'));
      case 'price':
        return books.sort((a, b) => a.price - b.price);
      default:
        return books;
    }
  });

  private readonly pageSize = 12;
  protected readonly visibleCount = signal(this.pageSize);
  protected readonly visibleBooks = computed(() => this.books().slice(0, this.visibleCount()));

  protected showMore(): void {
    this.visibleCount.update((count) => count + this.pageSize);
  }
}
