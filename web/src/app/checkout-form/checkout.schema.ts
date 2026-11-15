import {
  applyEach, applyWhen, disabled, email, hidden, min, minLength,
  pattern, required, schema, validate, validateHttp
} from '@angular/forms/signals';
import { environment } from '../../environments/environment';
import { CheckoutModel } from './checkout.model';

export const MIN_INSTITUTIONAL_BOOKS = 20;

interface Coupon {
  code: string;
  percent: number;
}

export const checkoutSchema = schema<CheckoutModel>((order) => {
  required(order.fullName, { message: 'Full name is required' });
  minLength(order.fullName, 3, { message: 'Enter at least 3 characters' });
  required(order.email, { message: 'Email is required' });
  email(order.email, { message: 'Enter a valid email' });

  hidden(order.address, { when: ({ valueOf }) => valueOf(order.shipping) === 'pickup' });
  required(order.address.city, { message: 'City is required' });
  required(order.address.street, { message: 'Street is required' });
  pattern(order.address.zip, /^[0-9]{5}$/, { message: 'Postal code must be 5 digits' });

  applyEach(order.lines, (line) => {
    min(line.quantity, 1, { message: 'Quantity must be at least 1' });
  });

  applyWhen(order, ({ valueOf }) => valueOf(order.institutional), (inst) => {
    required(inst.institutionName, { message: 'Institution name is required' });
    validate(inst.lines, ({ value }) => {
      const books = value().reduce((sum, line) => sum + line.quantity, 0);
      return books < MIN_INSTITUTIONAL_BOOKS
        ? { kind: 'minInstitutionalBooks', message: `Institutional orders need at least ${MIN_INSTITUTIONAL_BOOKS} books (now ${books})` }
        : null;
    });
  });

  disabled(order.couponCode, {
    when: ({ valueOf }) => valueOf(order.institutional) && 'Coupons do not apply to institutional orders'
  });
  validateHttp(order.couponCode, {
    request: ({ value }) =>
      value() ? `${environment.apiUrl}/coupons?code=${encodeURIComponent(value())}` : undefined,
    debounce: 400,
    onSuccess: (coupons: Coupon[]) =>
      coupons.length ? null : { kind: 'unknownCoupon', message: 'This coupon code is not valid' },
    onError: () => ({ kind: 'couponCheckFailed', message: 'Could not check the coupon, try again' })
  });
});
