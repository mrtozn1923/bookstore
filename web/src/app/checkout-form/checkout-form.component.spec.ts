import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CheckoutFormComponent } from './checkout-form.component';

describe('CheckoutFormComponent', () => {
  let fixture: ComponentFixture<CheckoutFormComponent>;
  let http: HttpTestingController;
  let el: HTMLElement;

  const type = (selector: string, value: string) => {
    const input = el.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
  };
  const click = (selector: string) => el.querySelector<HTMLElement>(selector)!.click();
  const select = (selector: string, value: string) => {
    const s = el.querySelector<HTMLSelectElement>(selector)!;
    s.value = value;
    s.dispatchEvent(new Event('input'));
    s.dispatchEvent(new Event('change'));
  };
  const errors = () => Array.from(el.querySelectorAll('.error')).map((p) => p.textContent!.trim());
  const wait = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));
  const settle = async () => {
    await wait(0);
    await fixture.whenStable();
    fixture.detectChanges();
  };
  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [CheckoutFormComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    fixture = TestBed.createComponent(CheckoutFormComponent);
    http = TestBed.inject(HttpTestingController);
    el = fixture.nativeElement;
    await settle();
  });

  it('shows errors only after the field is touched', async () => {
    expect(errors()).toEqual([]);
    type('#email', 'ada');
    await settle();
    expect(errors()).toEqual(['Enter a valid email']);
  });

  it('shows every error after a submit attempt', async () => {
    click('button[type=submit]');
    await settle();
    expect(errors()).toEqual([
      'Full name is required',
      'Email is required',
      'City is required',
      'Street is required'
    ]);
    http.expectNone(() => true);
  });

  it('hides the address for store pickup', async () => {
    select('#shipping', 'pickup');
    await settle();
    expect(el.querySelector('fieldset')).toBeNull();
    type('#fullName', 'Ada Lovelace');
    type('#email', 'ada@example.com');
    await settle();
    expect(fixture.componentInstance['checkoutForm']().valid()).toBe(true);
  });

  it('applies the institutional rules only for institutional orders', async () => {
    click('input[type=checkbox]');
    await settle();
    const coupon = el.querySelector<HTMLInputElement>('#coupon')!;
    expect(coupon.disabled).toBe(true);
    expect(el.querySelector('.hint')!.textContent).toBe('Coupons do not apply to institutional orders');

    const quantities = el.querySelectorAll<HTMLInputElement>('input[type=number]');
    quantities[0].value = '15';
    quantities[0].dispatchEvent(new Event('input'));
    quantities[0].dispatchEvent(new FocusEvent('focusout', { bubbles: true }));
    await settle();
    expect(errors()).toContain('Institutional orders need at least 20 books (now 16)');

    quantities[1].value = '5';
    quantities[1].dispatchEvent(new Event('input'));
    await settle();
    expect(errors()).not.toContain('Institutional orders need at least 20 books (now 16)');
  });

  it('checks the coupon once after typing stops', async () => {
    const coupon = el.querySelector<HTMLInputElement>('#coupon')!;
    for (const value of ['B', 'BO', 'BOO', 'BOOK5']) {
      coupon.value = value;
      coupon.dispatchEvent(new Event('input'));
      fixture.detectChanges();
      await wait(50);
    }
    coupon.dispatchEvent(new Event('blur'));
    http.expectNone(() => true);
    await wait(400);
    fixture.detectChanges();
    const req = http.expectOne('http://localhost:3000/coupons?code=BOOK5');
    fixture.detectChanges();
    expect(el.textContent).toContain('Checking the coupon...');
    req.flush([]);
    await settle();
    expect(errors()).toEqual(['This coupon code is not valid']);
  });

  it('puts server errors on the matching field', async () => {
    type('#fullName', 'Ada Lovelace');
    type('#email', 'ada@example.com');
    type('[placeholder=City]', 'Izmir');
    type('[placeholder=Street]', 'Kordon 1');
    type('[placeholder="Postal code"]', '35000');
    click('button[type=submit]');
    const req = http.expectOne('http://localhost:3000/orders');
    expect(req.request.body.address.zip).toBe('35000');
    req.flush(
      { errors: [{ field: 'address.zip', message: 'We do not deliver to 35000 yet' }] },
      { status: 422, statusText: 'Unprocessable Entity' }
    );
    await settle();
    expect(errors()).toEqual(['We do not deliver to 35000 yet']);
  });
});
