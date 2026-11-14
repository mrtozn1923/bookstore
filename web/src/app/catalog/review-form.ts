import { Component, output, signal } from '@angular/core';
import { FieldTree, form, FormField, FormRoot, min } from '@angular/forms/signals';
import { RatingInput } from '../shared/rating-input';

export interface Review {
  book: number;
  delivery: number;
  comment: string;
}

@Component({
  selector: 'app-review-form',
  imports: [FormField, FormRoot, RatingInput],
  template: `
    <form [formRoot]="reviewForm" aria-label="Review your order">
      <app-rating-input label="Rate the book" [formField]="reviewForm.book" />
      @if (errorFor(reviewForm.book); as message) {
        <p class="error">{{ message }}</p>
      }
      <app-rating-input label="Rate the delivery" [formField]="reviewForm.delivery" />
      @if (errorFor(reviewForm.delivery); as message) {
        <p class="error">{{ message }}</p>
      }
      <label for="comment">Comment</label>
      <textarea id="comment" [formField]="reviewForm.comment"></textarea>
      <button type="submit">Send review</button>
    </form>
  `,
})
export class ReviewForm {
  readonly send = output<Review>();

  private readonly review = signal<Review>({ book: 0, delivery: 0, comment: '' });
  protected readonly reviewForm = form(
    this.review,
    (path) => {
      min(path.book, 1, { message: 'Rate the book.' });
      min(path.delivery, 1, { message: 'Rate the delivery.' });
    },
    {
      submission: {
        action: async (field) => {
          this.send.emit(field().value());
        },
      },
    },
  );

  protected errorFor(field: FieldTree<number>): string | undefined {
    const state = field();
    return state.touched() && state.invalid() ? state.errors()[0]?.message : undefined;
  }
}
