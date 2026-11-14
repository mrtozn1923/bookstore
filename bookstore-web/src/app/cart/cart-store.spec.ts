import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { patchState } from '@ngrx/signals';
import { Dispatcher } from '@ngrx/signals/events';
import { unprotected } from '@ngrx/signals/testing';
import { ordersApiEvents } from '../checkout/checkout-events';
import type { Book } from '../models';
import { CartStore } from './cart-store';

const dune: Book = { isbn: '9780441172719', title: 'Dune', author: 'Frank Herbert', price: 450, stock: 12, category: 'science-fiction' };
const emma: Book = { isbn: '9780141439587', title: 'Emma', author: 'Jane Austen', price: 320, stock: 8, category: 'classics' };

describe('CartStore', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  });

  it('adds one more copy when the same book is added again', () => {
    const cart = TestBed.inject(CartStore);
    cart.add(dune);
    cart.add(dune);
    cart.add(emma);
    expect(cart.items().map((i) => `${i.title} x ${i.quantity}`)).toEqual(['Dune x 2', 'Emma x 1']);
    expect(cart.count()).toBe(3);
    expect(cart.totalPrice()).toBe(1220);
  });

  it('saves a versioned copy after the effect runs', () => {
    const cart = TestBed.inject(CartStore);
    cart.add(dune);
    // effects run during change detection; in a test, TestBed.tick() runs them
    TestBed.tick();
    expect(JSON.parse(localStorage.getItem('bookstore.cart')!)).toEqual({
      version: 2,
      items: [{ isbn: dune.isbn, title: 'Dune', price: 450, quantity: 1 }],
      coupon: null,
    });
  });

  it('reads a cart saved in the old array format', () => {
    localStorage.setItem('bookstore.cart', JSON.stringify([{ isbn: dune.isbn, title: 'Dune', price: 450, quantity: 2 }]));
    const cart = TestBed.inject(CartStore);
    expect(cart.count()).toBe(2);
  });

  it('computes the discount from a coupon put into state with unprotected()', () => {
    const cart = TestBed.inject(CartStore);
    cart.add(dune);
    cart.add(emma);
    // no public setter for coupon (only applyCoupon over HTTP): prepare the state directly
    patchState(unprotected(cart), { coupon: { code: 'BOOKWEEK', percent: 10 } });
    expect(cart.discount()).toBe(77);
    expect(cart.payable()).toBe(693);
  });

  it('applies a coupon from the API and reports an unknown one', async () => {
    const cart = TestBed.inject(CartStore);
    const http = TestBed.inject(HttpTestingController);

    const ok = cart.applyCoupon('BOOKWEEK');
    http.expectOne('http://localhost:5280/api/coupons/BOOKWEEK').flush({ code: 'BOOKWEEK', percent: 10 });
    await ok;
    expect(cart.coupon()).toEqual({ code: 'BOOKWEEK', percent: 10 });

    const unknown = cart.applyCoupon('FREE50');
    expect(cart.isPending()).toBe(true);
    http.expectOne('http://localhost:5280/api/coupons/FREE50').flush(null, { status: 404, statusText: 'Not Found' });
    await unknown;
    expect(cart.coupon()).toBeNull();
    expect(cart.requestError()).toBe('HTTP 404');
    http.verify();
  });

  it('empties itself when an order is placed', () => {
    const cart = TestBed.inject(CartStore);
    cart.add(dune);
    TestBed.inject(Dispatcher).dispatch(
      ordersApiEvents.orderPlaced({ id: 1043, items: cart.items(), shipping: 'standard', total: 450 }),
    );
    expect(cart.count()).toBe(0);
  });
});
