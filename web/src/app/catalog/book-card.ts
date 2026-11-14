import { Component, input, output } from '@angular/core';
import { PricePipe } from '../shared/price-pipe';
import { Book } from './book';
import { RatingSummary } from './rating-summary';

@Component({
  selector: 'app-book-card',
  imports: [PricePipe, RatingSummary],
  template: `
    <article>
      <h3>{{ book().title }}</h3>
      <p class="author">{{ book().author }}</p>
      <p class="price">{{ book().price | price }}</p>
      @if (book().stock > 0) {
        <button type="button" (click)="addToCart.emit(book())">Add to cart</button>
      } @else {
        <p class="stock">Out of stock</p>
      }
      @defer (on viewport) {
        <app-rating-summary [isbn]="book().isbn" />
      } @placeholder {
        <p class="rating-placeholder">Ratings</p>
      }
    </article>
  `,
})
export class BookCard {
  readonly book = input.required<Book>();
  readonly addToCart = output<Book>();
}
