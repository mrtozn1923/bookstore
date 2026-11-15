'use client';

import { useActionState } from 'react';
import { addToCart, type AddToCartState } from './actions';

// The form posts to a Server Function; useActionState keeps the message it returns
export function AddToCartForm({ isbn }: { isbn: string }) {
  const [state, formAction, isPending] = useActionState<AddToCartState, FormData>(addToCart, null);
  return (
    <form action={formAction}>
      <input type="hidden" name="isbn" value={isbn} />
      <label>Copies <input name="quantity" type="number" min={1} max={10} defaultValue={1} /></label>{' '}
      <button type="submit" disabled={isPending}>{isPending ? 'Adding…' : 'Add to cart'}</button>
      {state && <p role="status">{state.message}</p>}
    </form>
  );
}
