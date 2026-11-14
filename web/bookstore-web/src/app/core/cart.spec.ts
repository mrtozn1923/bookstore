import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { Cart } from './cart';

describe('Cart', () => {
  beforeEach(() => localStorage.clear());

  it('adds up quantities of the same book', () => {
    const cart = TestBed.inject(Cart);
    cart.add({ isbn: '9780141439587', title: 'Emma', unitPrice: 249.9, currency: 'TRY' });
    cart.add({ isbn: '9780141439587', title: 'Emma', unitPrice: 249.9, currency: 'TRY' }, 2);

    expect(cart.lines()).toHaveLength(1);
    expect(cart.count()).toBe(3);
    expect(cart.total()).toBeCloseTo(749.7);
  });

  it('takes the current prices from a 409 answer', () => {
    const cart = TestBed.inject(Cart);
    cart.add({ isbn: '9780141439549', title: 'Middlemarch', unitPrice: 379, currency: 'TRY' });

    cart.applyPrices([{ isbn: '9780141439549', currentPrice: 389 }]);

    expect(cart.total()).toBe(389);
  });

  it('removes a line when its quantity drops to zero', () => {
    const cart = TestBed.inject(Cart);
    cart.add({ isbn: '9780141439587', title: 'Emma', unitPrice: 249.9, currency: 'TRY' });

    cart.setQuantity('9780141439587', 0);

    expect(cart.count()).toBe(0);
  });
});
