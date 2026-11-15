import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FieldTree, FormField, FormRoot, TreeValidationResult, form } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { FieldErrorsComponent } from '../field-errors/field-errors.component';
import { ApiFieldError, OrderApiService } from '../order-api.service';
import { CheckoutModel, emptyCheckout } from './checkout.model';
import { checkoutSchema } from './checkout.schema';

@Component({
  imports: [FormField, FormRoot, FieldErrorsComponent],
  selector: 'app-checkout-form',
  styleUrl: './checkout-form.component.css',
  templateUrl: './checkout-form.component.html',
})
export class CheckoutFormComponent {
  private readonly orderApi = inject(OrderApiService);
  protected readonly placedOrderId = signal<number | null>(null);
  protected readonly order = signal<CheckoutModel>(emptyCheckout());

  protected readonly checkoutForm = form(this.order, checkoutSchema, {
    submission: {
      action: async (f) => {
        try {
          const placed = await firstValueFrom(this.orderApi.place(f().value()));
          this.placedOrderId.set(placed.id);
          return undefined;
        } catch (error) {
          return toFieldErrors(error, f);
        }
      }
    }
  });
}

function toFieldErrors(error: unknown, f: FieldTree<CheckoutModel>): TreeValidationResult {
  if (error instanceof HttpErrorResponse && error.status === 422) {
    const targets: Record<string, FieldTree<unknown>> = {
      email: f.email,
      'address.zip': f.address.zip,
      couponCode: f.couponCode
    };
    return (error.error.errors as ApiFieldError[]).map((e) => ({
      kind: 'server',
      message: e.message,
      fieldTree: targets[e.field] ?? f
    }));
  }
  return { kind: 'server', message: 'The order could not be placed, try again' };
}
