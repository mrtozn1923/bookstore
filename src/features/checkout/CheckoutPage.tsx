import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { useId, type ReactNode } from 'react';
import { useFieldArray, useForm, useWatch, type FieldError } from 'react-hook-form';
import { useNavigate } from 'react-router';
import { useAuth } from '@/features/auth/useAuth';
import { cartApi, cartKey } from '@/features/cart/cartApi';
import { cartIdStore } from '@/features/cart/cartIdStore';
import { useCart } from '@/features/cart/useCart';
import { checkoutSchema, type CheckoutValues } from '@/features/checkout/checkoutSchema';
import { applyServerErrors } from '@/features/checkout/serverErrors';
import { ApiError } from '@/shared/api';
import { formatPrice } from '@/shared/format';

export function CheckoutPage() {
  const { user } = useAuth();
  const { cartId, lines } = useCart();
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const { register, control, handleSubmit, setError, formState: { errors, isSubmitting } } = useForm<CheckoutValues>({
    resolver: zodResolver(checkoutSchema),
    defaultValues: {
      name: user?.name ?? '',
      email: user?.email ?? '',
      address: { line1: '', city: '', postalCode: '' },
      lines: lines.map(({ isbn, title, unitPrice, quantity }) => ({ isbn, title, unitPrice, quantity })),
    },
  });
  // Like a FormArray: one row per order line, each with a stable id for the key
  const { fields, remove } = useFieldArray({ control, name: 'lines' });
  // useWatch subscribes to the lines only; watch() would make the React Compiler skip this component
  const total = useWatch({ control, name: 'lines' }).reduce((sum, line) => sum + line.unitPrice * (line.quantity || 0), 0);

  async function placeOrder(values: CheckoutValues) {
    try {
      // Bring the server cart in line with the edited lines, then order it
      for (const line of lines) {
        const edited = values.lines.find((l) => l.isbn === line.isbn);
        const saved = !edited ? await cartApi.remove(cartId!, line.isbn)
          : edited.quantity !== line.quantity ? await cartApi.setQuantity(cartId!, line.isbn, edited.quantity)
          : null;
        if (saved) {
          queryClient.setQueryData(cartKey(saved.id), saved);
        }
      }
      const order = await cartApi.placeOrder(cartId!, values.name, values.email);
      queryClient.removeQueries({ queryKey: ['cart'] });
      queryClient.invalidateQueries({ queryKey: ['book'] });
      cartIdStore.set(null);
      navigate(`/orders/${order.id}`);
    } catch (error) {
      if (error instanceof ApiError && error.problem) {
        applyServerErrors(error.problem, setError);
      } else {
        setError('root.server', { message: error instanceof Error ? error.message : String(error) });
      }
    }
  }

  return (
    <form noValidate onSubmit={handleSubmit(placeOrder)}>
      <h1>Checkout</h1>
      <fieldset>
        <legend>Contact</legend>
        <Field label="Name" error={errors.name}>{(props) => <input {...props} {...register('name')} autoComplete="name" />}</Field>
        <Field label="Email" error={errors.email}>{(props) => <input {...props} {...register('email')} type="email" autoComplete="email" />}</Field>
      </fieldset>
      <fieldset>
        <legend>Delivery address</legend>
        <Field label="Street and number" error={errors.address?.line1}>{(props) => <input {...props} {...register('address.line1')} autoComplete="address-line1" />}</Field>
        <Field label="City" error={errors.address?.city}>{(props) => <input {...props} {...register('address.city')} autoComplete="address-level2" />}</Field>
        <Field label="Postal code" error={errors.address?.postalCode}>{(props) => <input {...props} {...register('address.postalCode')} inputMode="numeric" autoComplete="postal-code" />}</Field>
      </fieldset>
      <fieldset>
        <legend>Books</legend>
        {fields.map((field, index) => (
          <div key={field.id} className="line">
            <Field label={`Quantity of ${field.title}`} error={errors.lines?.[index]?.quantity}>
              {(props) => <input {...props} {...register(`lines.${index}.quantity`, { valueAsNumber: true })} type="number" min={1} max={99} />}
            </Field>
            <button type="button" onClick={() => remove(index)}>Remove</button>
          </div>
        ))}
        {errors.lines?.root && <p className="error" role="alert">{errors.lines.root.message}</p>}
      </fieldset>
      <p className="total">Total: {formatPrice(total)}</p>
      {errors.root?.server && <p ref={focusOnAttach} tabIndex={-1} className="error" role="alert">{errors.root.server.message}</p>}
      <button type="submit" disabled={isSubmitting}>{isSubmitting ? 'Placing order…' : 'Place order'}</button>
    </form>
  );
}

// The submit button was focused; move focus to the message so keyboard and screen reader users get it.
// Returning a cleanup keeps React 19 from calling this ref with null (post 07).
function focusOnAttach(node: HTMLParagraphElement) {
  node.focus();
  return () => {};
}

type FieldProps = {
  label: string;
  error: FieldError | undefined;
  children: (props: { id: string; 'aria-invalid': boolean; 'aria-describedby'?: string }) => ReactNode;
};

// Label, input and error message wired together for screen readers
function Field({ label, error, children }: FieldProps) {
  const id = useId();
  return (
    <p className="field">
      <label htmlFor={id}>{label}</label>
      {children({ id, 'aria-invalid': error !== undefined, 'aria-describedby': error ? `${id}-error` : undefined })}
      {error && <span id={`${id}-error`} className="error">{error.message}</span>}
    </p>
  );
}
