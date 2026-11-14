import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CartView } from '../core/api-models';
import { CartPage } from './cart-page';
import { CartStore } from './cart-store';

describe('CartPage coupon description', () => {
  beforeEach(() => {
    localStorage.setItem('bookstore.cartId', 'cart-1');
    TestBed.configureTestingModule({
      imports: [CartPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => localStorage.clear());

  it('keeps the formatting but drops script-capable markup', async () => {
    const fixture = TestBed.createComponent(CartPage);
    const loading = TestBed.inject(CartStore).load();
    TestBed.inject(HttpTestingController).expectOne('/api/carts/cart-1').flush(cartWith(
      '<strong>15%</strong> off classics<img src="x" onerror="alert(document.cookie)">'));
    await loading;
    await fixture.whenStable();

    const description = (fixture.nativeElement as HTMLElement)
      .querySelector('[data-testid="coupon-description"]')!;
    expect(description.querySelector('strong')?.textContent).toBe('15%');
    expect(description.querySelector('img[onerror]')).toBeNull();
  });
});

function cartWith(couponDescription: string): CartView {
  return {
    id: 'cart-1',
    items: [{ isbn: '9780141439587', title: 'Emma', unitPrice: 249.9, quantity: 1, lineTotal: 249.9 }],
    subtotal: 249.9,
    discount: 37.49,
    total: 212.41,
    currency: 'TRY',
    couponCode: 'CLASSIC15',
    couponDescription,
  };
}
