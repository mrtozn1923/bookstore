import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-book-rating',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span *ngFor="let star of stars" [class.filled]="star <= value">★</span>
  `
})
export class BookRatingComponent {
  @Input() value = 0;
  stars = [1, 2, 3, 4, 5];
}
