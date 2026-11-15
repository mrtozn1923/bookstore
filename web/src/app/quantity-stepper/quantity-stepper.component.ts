import { Component, computed, input, model, output } from '@angular/core';
import { FormValueControl, ValidationError } from '@angular/forms/signals';

@Component({
  selector: 'app-quantity-stepper',
  styleUrl: './quantity-stepper.component.css',
  host: { role: 'group', '[attr.aria-label]': 'label()', '(focusout)': 'onFocusOut($event)' },
  template: `
    <button type="button" aria-label="Decrease quantity"
            [disabled]="disabled() || atMin()" (click)="step(-1)">−</button>
    <input type="number" [attr.aria-label]="label()" [value]="value()"
           [disabled]="disabled()" [attr.aria-invalid]="invalid()"
           (input)="onInput($event)" />
    <button type="button" aria-label="Increase quantity"
            [disabled]="disabled() || atMax()" (click)="step(1)">+</button>
  `
})
export class QuantityStepperComponent implements FormValueControl<number> {
  // Signal Forms fills these from the field state
  readonly value = model(1);
  readonly disabled = input(false);
  readonly invalid = input(false);
  readonly errors = input<readonly ValidationError[]>([]);
  readonly min = input<number | undefined>(undefined);
  readonly max = input<number | undefined>(undefined);
  readonly touch = output<void>();

  readonly label = input('Quantity');
  protected readonly atMin = computed(() => this.min() !== undefined && this.value() <= this.min()!);
  protected readonly atMax = computed(() => this.max() !== undefined && this.value() >= this.max()!);

  protected step(delta: number): void {
    this.value.update((q) => q + delta);
  }

  protected onInput(event: Event): void {
    const raw = (event.target as HTMLInputElement).valueAsNumber;
    this.value.set(Number.isNaN(raw) ? 0 : raw);
  }

  protected onFocusOut(event: FocusEvent): void {
    // focus left the whole component: report touched
    const host = event.currentTarget as HTMLElement;
    if (!host.contains(event.relatedTarget as Node | null)) {
      this.touch.emit();
    }
  }
}
