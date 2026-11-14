import { Component } from '@angular/core';
import { NgForm, FormsModule } from '@angular/forms';
import { JsonPipe } from '@angular/common';

@Component({
    selector: 'app-checkout',
    templateUrl: './checkout.component.html',
    styleUrls: ['./checkout.component.css'],
    standalone: true,
    imports: [FormsModule, JsonPipe]
})
export class CheckoutComponent {
  onSubmit(form: NgForm): void {
    console.log(form.value);
    form.resetForm();
  }
}
