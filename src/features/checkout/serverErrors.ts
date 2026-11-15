import type { FieldPath, UseFormSetError } from 'react-hook-form';
import type { CheckoutValues } from './checkoutSchema';
import type { ProblemDetails } from '@/shared/api';

// Bookstore.Api reports errors by request property name (PlaceOrderRequest); the form uses its own names
const fieldFor: Record<string, FieldPath<CheckoutValues>> = {
  CustomerName: 'name',
  CustomerEmail: 'email',
};

export function applyServerErrors(problem: ProblemDetails, setError: UseFormSetError<CheckoutValues>) {
  const unmatched: string[] = [];
  for (const [key, messages] of Object.entries(problem.errors ?? {})) {
    const field = fieldFor[key];
    if (field) {
      setError(field, { type: 'server', message: messages.join(' ') }, { shouldFocus: true });
    } else {
      unmatched.push(...messages);
    }
  }
  // Anything without a field of its own (409 out of stock, unknown keys) goes to the form-level error
  const formMessage = unmatched.length > 0 ? unmatched.join(' ') : problem.errors ? '' : (problem.detail ?? problem.title ?? '');
  if (formMessage) {
    setError('root.server', { type: 'server', message: formMessage });
  }
}
