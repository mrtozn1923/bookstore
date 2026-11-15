import { Component, booleanAttribute, computed, input, model, output, ChangeDetectionStrategy } from '@angular/core';
import { FormValueControl } from '@angular/forms/signals';

@Component({
  selector: 'app-book-rating',
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    @for (star of stars(); track star) {
      <button type="button" (click)="rate(star)" (blur)="touch.emit()" [class.filled]="star <= value()"
              [class.compact]="compact()" [attr.aria-label]="'Rate ' + star + ' of ' + starCount()">★</button>
    }
  `
})
export class BookRatingComponent implements FormValueControl<number> {
  starCount = input(5);
  compact = input(false, { transform: booleanAttribute });
  disabled = input(false, { transform: booleanAttribute });
  value = model(0);
  rated = output<number>();
  touch = output<void>();

  stars = computed(() => Array.from({ length: this.starCount() }, (_, i) => i + 1));

  rate(star: number): void {
    if (this.disabled()) {
      return;
    }
    this.value.set(star);
    this.rated.emit(star);
  }
}
