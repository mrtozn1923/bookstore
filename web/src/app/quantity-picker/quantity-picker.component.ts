import { Component, model, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'app-quantity-picker',
    templateUrl: './quantity-picker.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrls: ['./quantity-picker.component.css'],
})
export class QuantityPickerComponent {
  quantity = model(1);

  change(delta: number): void {
    this.quantity.update(q => Math.max(1, q + delta));
  }

  reset(): void {
    this.quantity.set(1);
  }
}
