import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { dune, hobbit } from '../../testing/books';
import { CartStore } from './cart-store';
import { CartSummary } from './cart-summary';

@Component({
  imports: [CartSummary],
  template: `
    <app-cart-summary>
      <button cartActions type="button">Checkout</button>
    </app-cart-summary>
  `,
})
class CartSummaryHost {}

describe('CartSummary', () => {
  function text(selector: string): string | undefined {
    return TestBed.getLastFixture().nativeElement.querySelector(selector)?.textContent;
  }

  it('says the cart is empty and hides the projected actions', async () => {
    const fixture = TestBed.createComponent(CartSummaryHost);
    await fixture.whenStable();

    expect(text('.empty')).toBe('Your cart is empty.');
    expect(text('button')).toBeUndefined();
  });

  it('lists the lines with the total and shows the projected actions', async () => {
    const cart = TestBed.inject(CartStore);
    cart.add(dune);
    cart.add(dune);
    cart.add(hobbit);

    const fixture = TestBed.createComponent(CartSummaryHost);
    await fixture.whenStable();

    const titles = [...fixture.nativeElement.querySelectorAll('.title')].map((title) => title.textContent);
    expect(titles).toEqual(['Dune', 'The Hobbit']);
    expect(text('.total')).toBe('Total: ₺1.280,00');
    expect(text('button[cartActions]')).toBe('Checkout');
  });

  it('updates the total when a quantity changes', async () => {
    TestBed.inject(CartStore).add(dune);
    const fixture = TestBed.createComponent(CartSummaryHost);
    await fixture.whenStable();

    fixture.nativeElement.querySelector('button[aria-label="Increase quantity"]').click();
    await fixture.whenStable();

    expect(text('.line-total')).toBe('₺900,00');
    expect(text('.total')).toBe('Total: ₺900,00');
  });
});
