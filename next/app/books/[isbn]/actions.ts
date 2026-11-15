'use server';

import { cookies } from 'next/headers';
import * as z from 'zod';
import { addToServerCart } from '@/lib/cart';

export type AddToCartState = { message: string } | null;

const addToCartInput = z.object({
  isbn: z.string().regex(/^\d{13}$/),
  quantity: z.coerce.number().int().min(1).max(10),
});

// A Server Function is a public POST endpoint: anyone can call it with any body, not only this form
export async function addToCart(_previous: AddToCartState, formData: FormData): Promise<AddToCartState> {
  const input = addToCartInput.safeParse({ isbn: formData.get('isbn'), quantity: formData.get('quantity') });
  if (!input.success) {
    return { message: 'Invalid request.' };
  }
  const cookieStore = await cookies();
  const cart = await addToServerCart(cookieStore.get('cartId')?.value, input.data.isbn, input.data.quantity);
  // The cart id stays on the server side of the wire: an HttpOnly cookie, not localStorage
  cookieStore.set('cartId', cart.id, { httpOnly: true, sameSite: 'lax', path: '/' });
  const count = cart.items.reduce((sum, line) => sum + line.quantity, 0);
  return { message: `Added. Your cart has ${count} item(s).` };
}
