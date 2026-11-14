import { Component, booleanAttribute, computed, input, model, output, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-book-rating',
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    @for (star of stars(); track star) {
      <button (click)="rate(star)" [class.filled]="star <= value()" [class.compact]="compact()">★</button>
    }
  `
})
export class BookRatingComponent {
  max = input(5);
  compact = input(false, { transform: booleanAttribute });
  disabled = input(false, { transform: booleanAttribute });
  value = model(0);
  rated = output<number>();

  stars = computed(() => Array.from({ length: this.max() }, (_, i) => i + 1));

  rate(star: number): void {
    if (this.disabled()) {
      return;
    }
    this.value.set(star);
    this.rated.emit(star);
  }
}
