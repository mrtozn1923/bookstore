import { TestBed } from '@angular/core/testing';
import { PriceTag } from './price-tag';

describe('PriceTag', () => {
  it('formats the price with its currency', async () => {
    const fixture = TestBed.createComponent(PriceTag);
    fixture.componentRef.setInput('price', { amount: 420, currency: 'TRY' });
    fixture.componentRef.setInput('listPrice', { amount: 480, currency: 'TRY' });
    await fixture.whenStable();
    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.price')?.textContent).toBe('TRY\u00a0420.00');
    expect(host.querySelector('.list-price')?.textContent).toBe('TRY\u00a0480.00');
  });
});
