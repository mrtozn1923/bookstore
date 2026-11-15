import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { TestBed } from '@angular/core/testing';
import { MatSnackBarHarness } from '@angular/material/snack-bar/testing';
import { provideRouter } from '@angular/router';
import { CartView } from '../core/api-models';
import { CartPage } from './cart-page';
import { CartStore } from './cart-store';

const cart: CartView = {
  id: 'cart-1', currency: 'TRY', total: 978,
  items: [{ isbn: '9780441172719', title: 'Dune', unitPrice: 489, quantity: 2, lineTotal: 978 }],
};

describe('CartPage', () => {
  it('removes a book and puts the same quantity back on Undo', async () => {
    TestBed.configureTestingModule({
      imports: [CartPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    localStorage.setItem('bookstore.cartId', 'cart-1');
    const store = TestBed.inject(CartStore);
    const http = TestBed.inject(HttpTestingController);
    const loading = store.load();
    http.expectOne('/api/carts/cart-1').flush(cart);
    await loading;

    const fixture = TestBed.createComponent(CartPage);
    await fixture.whenStable();
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('table.cart button')!.click();
    http.expectOne(r => r.method === 'DELETE').flush({ ...cart, items: [], total: 0 });
    await fixture.whenStable();

    const snackBar = await TestbedHarnessEnvironment.documentRootLoader(fixture).getHarness(MatSnackBarHarness);
    expect(await snackBar.getMessage()).toBe('Removed Dune');
    await snackBar.dismissWithAction();
    const undo = http.expectOne(r => r.method === 'PUT');
    expect(undo.request.url).toBe('/api/carts/cart-1/items/9780441172719');
    expect(undo.request.body).toEqual({ quantity: 2 });
    localStorage.clear();
  });
});
