import { TestBed } from '@angular/core/testing';

import { CartService } from './cart.service';
import { CheckoutService } from './checkout.service';

describe('CheckoutService', () => {
  let service: CheckoutService;
  let cartSpy: jasmine.SpyObj<CartService>;

  beforeEach(() => {
    cartSpy = jasmine.createSpyObj('CartService', ['getTotal']);

    TestBed.configureTestingModule({
      providers: [{ provide: CartService, useValue: cartSpy }]
    });
    service = TestBed.inject(CheckoutService);
  });

  it('should offer free shipping for orders over 500 TRY', () => {
    cartSpy.getTotal.and.returnValue(650);

    expect(service.getShippingFee()).toBe(0);
    expect(cartSpy.getTotal).toHaveBeenCalled();
  });
});
