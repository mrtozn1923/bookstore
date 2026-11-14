import { Component, computed, signal, ChangeDetectionStrategy } from '@angular/core';
import { NgForm, FormsModule } from '@angular/forms';
import { JsonPipe } from '@angular/common';
import { ShippingPickerComponent } from '../shipping-picker/shipping-picker.component';

@Component({
    selector: 'app-checkout',
    templateUrl: './checkout.component.html',
    styleUrls: ['./checkout.component.css'],
    changeDetection: ChangeDetectionStrategy.Eager,
    imports: [FormsModule, JsonPipe, ShippingPickerComponent]
})
export class CheckoutComponent {
  abroad = signal(false);
  shippingOptions = computed(() =>
    this.abroad() ? ['Standard', 'Express'] : ['Standard', 'Express', 'Same day']);

  onSubmit(form: NgForm): void {
    console.log(form.value);
    form.resetForm();
  }
}
