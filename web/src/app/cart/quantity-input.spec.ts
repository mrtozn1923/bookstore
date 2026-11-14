import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { QuantityInput } from './quantity-input';

@Component({
  imports: [QuantityInput],
  template: `<app-quantity-input [(quantity)]="quantity" [max]="3" />`,
})
class QuantityHost {
  readonly quantity = signal(2);
}

describe('QuantityInput', () => {
  function button(label: string): HTMLButtonElement {
    return TestBed.getLastFixture().nativeElement.querySelector(`button[aria-label="${label}"]`);
  }

  it('writes the new quantity back to the parent', async () => {
    const fixture = TestBed.createComponent(QuantityHost);
    await fixture.whenStable();

    button('Increase quantity').click();
    await fixture.whenStable();

    expect(fixture.componentInstance.quantity()).toBe(3);
    expect(fixture.nativeElement.querySelector('.quantity').textContent).toBe('3');
  });

  it('shows a quantity changed by the parent', async () => {
    const fixture = TestBed.createComponent(QuantityHost);
    await fixture.whenStable();

    fixture.componentInstance.quantity.set(1);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.quantity').textContent).toBe('1');
    expect(button('Decrease quantity').disabled).toBe(true);
  });

  it('does not go above the maximum', async () => {
    const fixture = TestBed.createComponent(QuantityHost);
    fixture.componentInstance.quantity.set(3);
    await fixture.whenStable();

    expect(button('Increase quantity').disabled).toBe(true);
  });
});
