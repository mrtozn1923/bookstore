import { Component, input, model, output } from '@angular/core';
import { FormCheckboxControl } from '@angular/forms/signals';

@Component({
  selector: 'app-gift-wrap-toggle',
  template: `
    <button type="button" role="switch" [attr.aria-checked]="checked()"
            [disabled]="disabled()" (click)="checked.set(!checked())" (blur)="touch.emit()">
      Gift wrap: {{ checked() ? 'on' : 'off' }}
    </button>
  `
})
export class GiftWrapToggleComponent implements FormCheckboxControl {
  readonly checked = model(false);
  readonly disabled = input(false);
  readonly touch = output<void>();
}
