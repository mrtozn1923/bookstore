import { computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CartView } from '../core/api-models';
import { CartPage } from './cart-page';
import { CartStore } from './cart-store';

describe('CartPage institutional minimum', () => {
  const cart = signal<CartView | null>({
    id: 'c1',
    items: [{ isbn: '9780141439587', title: 'Emma', unitPrice: 249.9, quantity: 5, lineTotal: 1249.5 }],
    total: 1249.5,
    currency: 'TRY',
  });
  const store = {
    cart,
    itemCount: computed(() => (cart()?.items ?? []).reduce((sum, line) => sum + line.quantity, 0)),
  };

  async function render() {
    await TestBed.configureTestingModule({
      imports: [CartPage],
      providers: [provideRouter([]), { provide: CartStore, useValue: store }],
    }).compileComponents();
    const fixture = TestBed.createComponent(CartPage);
    await fixture.whenStable();
    return fixture;
  }

  async function typeCode(fixture: Awaited<ReturnType<typeof render>>, code: string) {
    const input = fixture.nativeElement.querySelector('[data-testid="institution-code"]') as HTMLInputElement;
    input.value = code;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  it('explains the minimum and blocks checkout below 20 books', async () => {
    const fixture = await render();
    await typeCode(fixture, 'RHS');

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('[data-testid="institution-hint"]')?.textContent).toContain('Add 15 more');
    expect(element.querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBe(true);
  });

  it('does not limit orders without an institution code', async () => {
    const fixture = await render();
    await typeCode(fixture, '');

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('[data-testid="institution-hint"]')).toBeNull();
    expect(element.querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBe(false);
  });
});
