import { Component, forwardRef, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-legacy-quantity-stepper',
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => LegacyQuantityStepperComponent),
    multi: true
  }],
  template: `
    <button type="button" [disabled]="disabled()" (click)="step(-1)" (blur)="onTouched()">−</button>
    <span>{{ quantity() }}</span>
    <button type="button" [disabled]="disabled()" (click)="step(1)" (blur)="onTouched()">+</button>
  `
})
export class LegacyQuantityStepperComponent implements ControlValueAccessor {
  protected readonly quantity = signal(1);
  protected readonly disabled = signal(false);
  private onChange: (value: number) => void = () => {};
  protected onTouched: () => void = () => {};

  // form → component: update the view only, never call onChange
  writeValue(value: number | null): void {
    this.quantity.set(value ?? 1);
  }

  registerOnChange(fn: (value: number) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected step(delta: number): void {
    this.quantity.update((q) => Math.max(1, q + delta));
    // component → form
    this.onChange(this.quantity());
  }
}
