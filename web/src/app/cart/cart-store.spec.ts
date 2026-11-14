import { TestBed } from '@angular/core/testing';
import { dune, hobbit } from '../../testing/books';
import { CartStore } from './cart-store';

describe('CartStore', () => {
  it('increases the quantity when the same book is added again', () => {
    const cart = TestBed.inject(CartStore);

    cart.add(dune);
    cart.add(dune);

    expect(cart.lines()).toEqual([{ book: dune, quantity: 2 }]);
    expect(cart.count()).toBe(2);
  });

  it('keeps the quantity between 1 and the stock', () => {
    const cart = TestBed.inject(CartStore);
    cart.add(dune);

    cart.setQuantity(dune.isbn, 10);
    expect(cart.lines()[0].quantity).toBe(3);

    cart.setQuantity(dune.isbn, 0);
    expect(cart.lines()[0].quantity).toBe(1);
  });

  it('adds up the total of all lines', () => {
    const cart = TestBed.inject(CartStore);
    cart.add(dune);
    cart.add(hobbit);
    cart.add(hobbit);

    expect(cart.total()).toBe(450 + 2 * 380);
  });

  it('removes a line', () => {
    const cart = TestBed.inject(CartStore);
    cart.add(dune);
    cart.add(hobbit);

    cart.remove(dune.isbn);

    expect(cart.lines().map((line) => line.book.title)).toEqual(['The Hobbit']);
  });
});
