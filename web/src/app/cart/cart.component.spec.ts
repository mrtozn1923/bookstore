import { TestBed } from '@angular/core/testing';
import { CartService } from '../cart.service';
import { CartComponent } from './cart.component';

describe('CartComponent', () => {
  const rows = (el: HTMLElement) => Array.from(el.querySelectorAll('.cart-line span:first-child')).map((s) => s.textContent);

  it('removes a line right away when animations are off (the TestBed default)', async () => {
    const cart = TestBed.inject(CartService);
    cart.add({ title: 'Dune', price: 450 });
    cart.add({ title: 'Emma', price: 320 });
    const fixture = TestBed.createComponent(CartComponent);
    fixture.autoDetectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;
    expect(rows(el)).toEqual(['Dune', 'Emma']);
    expect(el.querySelector<HTMLElement>('.free-shipping')!.hidden).toBe(false);
    el.querySelector<HTMLButtonElement>('.cart-line button')!.click();
    await fixture.whenStable();
    expect(rows(el)).toEqual(['Emma']);
    expect(el.querySelector<HTMLElement>('.free-shipping')!.hidden).toBe(true);
  });
});
