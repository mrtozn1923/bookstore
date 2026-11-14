import { Component, Input, booleanAttribute, numberAttribute } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-book-rating',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span *ngFor="let star of stars" [class.filled]="star <= value" [class.compact]="compact">★</span>
  `
})
export class BookRatingComponent {
  @Input({ required: true, transform: numberAttribute }) value!: number;
  @Input({ transform: booleanAttribute }) compact = false;
  @Input({ transform: numberAttribute }) max = 5;

  get stars(): number[] {
    return Array.from({ length: this.max }, (_, i) => i + 1);
  }
}
