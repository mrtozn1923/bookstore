import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { dune } from '../../testing/books';
import { CheckoutForm } from '../checkout/checkout-form';
import { CartStore } from './cart-store';
import { CartPage } from './cart-page';

describe('CartPage', () => {
  it('takes the user to checkout', async () => {
    await render(CartPage, { routes: [{ path: 'checkout', component: CheckoutForm }] });
    TestBed.inject(CartStore).add(dune);
    const user = userEvent.setup();

    await user.click(await screen.findByRole('link', { name: 'Checkout' }));

    expect(TestBed.inject(Router).url).toBe('/checkout');
  });
});
