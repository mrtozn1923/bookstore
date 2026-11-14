import { Component, computed, input, model, output } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';

@Component({
  selector: 'app-rating-input',
  template: `
    <fieldset (keydown)="onKeydown($event)">
      <legend>{{ label() }}</legend>
      @for (star of stars(); track star) {
        <label [class.filled]="star <= value()">
          <input
            type="radio"
            [name]="label()"
            [checked]="star === value()"
            [disabled]="disabled()"
            (change)="select(star)"
          />
          <span class="visually-hidden">{{ star === 1 ? '1 star' : star + ' stars' }}</span>
          <span aria-hidden="true">★</span>
        </label>
      }
    </fieldset>
  `,
  styles: `
    fieldset { border: 0; padding: 0; }
    label { font-size: 1.5rem; color: #c4c4c4; cursor: pointer; }
    label.filled { color: #f5a623; }
    input, .visually-hidden { position: absolute; width: 1px; height: 1px; overflow: hidden; clip-path: inset(50%); }
  `,
})
export class RatingInput implements FormValueControl<number> {
  readonly value = model(0);
  readonly label = input.required<string>();
  readonly count = input(5);
  readonly disabled = input(false);
  readonly touch = output<void>();

  protected readonly stars = computed(() => Array.from({ length: this.count() }, (_, index) => index + 1));

  protected select(star: number): void {
    this.value.set(star);
    this.touch.emit();
  }

  protected onKeydown(event: KeyboardEvent): void {
    const step = { ArrowRight: 1, ArrowUp: 1, ArrowLeft: -1, ArrowDown: -1 }[event.key];
    if (step === undefined || this.disabled()) {
      return;
    }
    event.preventDefault();
    const next = Math.min(Math.max(this.value() + step, 1), this.count());
    this.select(next);
    (event.currentTarget as HTMLElement).querySelectorAll('input')[next - 1].focus();
  }
}
