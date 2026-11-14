import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-quantity-picker',
  templateUrl: './quantity-picker.component.html',
  styleUrls: ['./quantity-picker.component.css']
})
export class QuantityPickerComponent {
  @Input() quantity = 1;
  @Output() quantityChange = new EventEmitter<number>();

  change(delta: number): void {
    this.quantity = Math.max(1, this.quantity + delta);
    this.quantityChange.emit(this.quantity);
  }

  reset(): void {
    this.quantity = 1;
    this.quantityChange.emit(this.quantity);
  }
}
