import { Component, input, linkedSignal, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-shipping-picker',
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    @for (option of options(); track option) {
      <button type="button" (click)="selected.set(option)">{{ option }}</button>
    }
    <p>Selected: {{ selected() }}</p>
  `
})
export class ShippingPickerComponent {
  options = input<string[]>([]);
  selected = linkedSignal<string[], string>({
    source: this.options,
    computation: (opts, previous) =>
      previous && opts.includes(previous.value) ? previous.value : opts[0]
  });
}
