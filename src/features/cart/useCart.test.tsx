import { act, renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { expect, it } from 'vitest';
import { useCart } from '@/features/cart/useCart';
import { testBooks } from '@/test/books';
import { server } from '@/test/server';
import { createTestQueryClient, withQueryClient } from '@/test/render';

const [dune, emma] = testBooks;

it('creates a server cart on the first add and shows the saved lines', async () => {
  const { result } = renderHook(() => useCart(), { wrapper: withQueryClient(createTestQueryClient()) });

  act(() => result.current.add(dune, 2));

  await waitFor(() => expect(result.current.itemCount).toBe(2));
  expect(result.current.cartId).not.toBeNull();
  expect(result.current.total).toBe(978);
});

it('rolls back the optimistic change and exposes the error when the server refuses it', async () => {
  const { result } = renderHook(() => useCart(), { wrapper: withQueryClient(createTestQueryClient()) });
  act(() => result.current.add(dune, 1));
  await waitFor(() => expect(result.current.itemCount).toBe(1));

  // Only for this test: the API answers 409, but only when the test lets it
  let answer = () => {};
  const answered = new Promise<void>((resolve) => { answer = resolve; });
  server.use(http.put('/api/carts/:cartId/items/:isbn', async () => {
    await answered;
    return HttpResponse.json({ title: 'Not enough stock', status: 409 },
      { status: 409, headers: { 'Content-Type': 'application/problem+json' } });
  }));
  act(() => result.current.add(emma, 1));

  // Optimistic: Emma is in the cart before the server has answered
  await waitFor(() => expect(result.current.itemCount).toBe(2));
  answer();
  await waitFor(() => expect(result.current.error?.message).toBe('Not enough stock'));
  expect(result.current.lines.map((line) => line.title)).toEqual(['Dune']);
});
