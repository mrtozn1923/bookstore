import { describe, expect, it } from 'vitest';
import { applyChange } from './cartLines';
import type { Cart } from './types';

const cart: Cart = {
  id: 'cart-1',
  currency: 'TRY',
  items: [{ isbn: '9780441172719', title: 'Dune', unitPrice: 489, quantity: 1, lineTotal: 489 }],
  total: 489,
};

describe('applyChange', () => {
  it('adds a new book, keeps the lines sorted by title and recalculates the total', () => {
    const next = applyChange(cart, { isbn: '9780141439587', title: 'Emma', unitPrice: 249.9, quantity: 2 });

    expect(next.items.map((line) => line.title)).toEqual(['Dune', 'Emma']);
    expect(next.items[1].lineTotal).toBeCloseTo(499.8);
    expect(next.total).toBeCloseTo(988.8);
  });

  it('changes the quantity of a book that is already in the cart', () => {
    const next = applyChange(cart, { isbn: '9780441172719', title: 'Dune', unitPrice: 489, quantity: 3 });

    expect(next.items).toHaveLength(1);
    expect(next.total).toBe(1467);
  });

  it('removes the line when the quantity is 0', () => {
    expect(applyChange(cart, { isbn: '9780441172719', title: 'Dune', unitPrice: 489, quantity: 0 }).items).toEqual([]);
  });

  it('does not touch the cart it was given', () => {
    const frozen = Object.freeze({ ...cart, items: Object.freeze([...cart.items]) as Cart['items'] });

    applyChange(frozen, { isbn: '9780141439587', title: 'Emma', unitPrice: 249.9, quantity: 1 });

    expect(frozen.items).toHaveLength(1);
  });
});
