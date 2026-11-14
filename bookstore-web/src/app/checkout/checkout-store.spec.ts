import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CartStore } from '../cart/cart-store';
import { CheckoutStore } from './checkout-store';

describe('CheckoutStore', () => {
  // the dependency is replaced by a fake with only what CheckoutStore reads
  const payable = signal(450);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CheckoutStore, { provide: CartStore, useValue: { payable } }],
    });
  });

  it('charges standard shipping below 500 TRY and nothing above', () => {
    const checkout = TestBed.inject(CheckoutStore);
    expect(checkout.shippingPrice()).toBe(40);
    payable.set(620);
    expect(checkout.shippingPrice()).toBe(0);
  });

  it('charges 90 TRY for express shipping', () => {
    const checkout = TestBed.inject(CheckoutStore);
    checkout.chooseShipping('express');
    expect(checkout.payable()).toBe(620 + 90);
  });
});
