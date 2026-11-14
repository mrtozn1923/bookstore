import { Component, computed, inject, signal } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { CartStore } from '../cart/cart-store';
import { BookCard } from './book-card';
import { CatalogService } from './catalog-service';

@Component({
  selector: 'app-catalog-page',
  imports: [BookCard, MatFormFieldModule, MatSelectModule],
  template: `
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
          @for (book of books(); track book.isbn) {
            <li><app-book-card [book]="book" (addToCart)="cart.add($event)" /></li>
          } @empty {
            <li>No books found.</li>
          }
        </ul>
      }
    }
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
}
