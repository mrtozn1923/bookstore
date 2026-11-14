import { Component, output, signal } from '@angular/core';
import { email, FieldTree, form, FormField, FormRoot, required } from '@angular/forms/signals';

export interface CheckoutDetails {
  fullName: string;
  email: string;
  city: string;
}

@Component({
  selector: 'app-checkout-form',
  imports: [FormField, FormRoot],
  template: `
    <form [formRoot]="checkoutForm" aria-labelledby="checkout-title">
      <h2 id="checkout-title">Shipping details</h2>

      <label for="fullName">Full name</label>
      <input
        id="fullName"
        autocomplete="name"
        [formField]="checkoutForm.fullName"
        [attr.aria-invalid]="errorFor(checkoutForm.fullName) ? true : null"
        [attr.aria-describedby]="errorFor(checkoutForm.fullName) ? 'fullName-error' : null"
      />
      @if (errorFor(checkoutForm.fullName); as message) {
        <p id="fullName-error" class="error">{{ message }}</p>
      }

      <label for="email">Email</label>
      <input
        id="email"
        type="email"
        autocomplete="email"
        [formField]="checkoutForm.email"
        [attr.aria-invalid]="errorFor(checkoutForm.email) ? true : null"
        [attr.aria-describedby]="errorFor(checkoutForm.email) ? 'email-error' : null"
      />
      @if (errorFor(checkoutForm.email); as message) {
        <p id="email-error" class="error">{{ message }}</p>
      }

      <label for="city">City</label>
      <select
        id="city"
        [formField]="checkoutForm.city"
        [attr.aria-invalid]="errorFor(checkoutForm.city) ? true : null"
        [attr.aria-describedby]="errorFor(checkoutForm.city) ? 'city-error' : null"
      >
        <option value="">Choose…</option>
        <option value="Ankara">Ankara</option>
        <option value="Istanbul">Istanbul</option>
        <option value="Izmir">Izmir</option>
      </select>
      @if (errorFor(checkoutForm.city); as message) {
        <p id="city-error" class="error">{{ message }}</p>
      }

      <button type="submit">Place order</button>
    </form>
  `,
})
export class CheckoutForm {
  readonly placeOrder = output<CheckoutDetails>();

  private readonly details = signal<CheckoutDetails>({ fullName: '', email: '', city: '' });
  protected readonly checkoutForm = form(
    this.details,
    (path) => {
      required(path.fullName, { message: 'Enter your full name.' });
      required(path.email, { message: 'Enter your email address.' });
      email(path.email, { message: 'Enter a valid email address.' });
      required(path.city, { message: 'Choose a city.' });
    },
    {
      submission: {
        action: async (field) => {
          this.placeOrder.emit(field().value());
        },
      },
    },
  );

  protected errorFor(field: FieldTree<string>): string | undefined {
    const state = field();
    return state.touched() && state.invalid() ? state.errors()[0]?.message : undefined;
  }
}
