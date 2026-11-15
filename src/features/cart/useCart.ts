import { useMutation, useMutationState, useQuery, useQueryClient } from '@tanstack/react-query';
import { cartApi, cartKey, cartQuery } from '@/features/cart/cartApi';
import { useCartId } from '@/features/cart/useCartId';
import { applyChange, type QuantityChange } from '@/features/cart/cartLines';
import type { Cart, CartLine } from '@/features/cart/types';

const quantityMutationKey = ['cart', 'setQuantity'];

export function useCart() {
  const { cartId, setCartId } = useCartId();
  const queryClient = useQueryClient();

  const { data: cart = null } = useQuery({ ...cartQuery(cartId), enabled: cartId !== null });

  const quantityMutation = useMutation({
    mutationKey: quantityMutationKey,
    mutationFn: async (change: QuantityChange) => {
      let id = cartId;
      if (id === null || cart === null) {
        const created = await cartApi.create();
        // Seed the cache with the new (empty) cart so switching the key does not trigger a GET
        queryClient.setQueryData(cartKey(created.id), created);
        setCartId(created.id);
        id = created.id;
      }
      return change.quantity > 0
        ? cartApi.setQuantity(id, change.isbn, change.quantity)
        : cartApi.remove(id, change.isbn);
    },
    // Optimistic update: change the cached cart now, keep the old one for a rollback
    onMutate: async (change) => {
      if (cartId === null || cart === null) {
        return { previous: undefined };
      }
      await queryClient.cancelQueries({ queryKey: cartKey(cartId) });
      const previous = queryClient.getQueryData<Cart | null>(cartKey(cartId)) ?? undefined;
      if (previous) {
        queryClient.setQueryData(cartKey(cartId), applyChange(previous, change));
      }
      return { previous };
    },
    onError: (_error, _change, result) => {
      if (result?.previous) {
        queryClient.setQueryData(cartKey(result.previous.id), result.previous);
      }
    },
    onSuccess: (saved) => queryClient.setQueryData(cartKey(saved.id), saved),
  });

  // Mutation state belongs to the hook instance that called mutate(); read the latest one from the cache instead
  const latest = useMutationState({ filters: { mutationKey: quantityMutationKey }, select: (m) => m.state }).at(-1);

  const lines = cart?.items ?? [];
  const quantityOf = (isbn: string) => lines.find((line) => line.isbn === isbn)?.quantity ?? 0;
  const setQuantity = (line: Omit<CartLine, 'lineTotal'>) => quantityMutation.mutate(line);

  return {
    cartId: cart?.id ?? null,
    lines,
    itemCount: lines.reduce((sum, line) => sum + line.quantity, 0),
    total: cart?.total ?? 0,
    quantityOf,
    error: latest?.status === 'error' ? latest.error : null,
    add: (book: { isbn: string; title: string; price: number }, amount: number) =>
      setQuantity({ isbn: book.isbn, title: book.title, unitPrice: book.price, quantity: quantityOf(book.isbn) + amount }),
    setQuantity,
    remove: (line: CartLine) => setQuantity({ ...line, quantity: 0 }),
    restore: setQuantity,
  };
}
