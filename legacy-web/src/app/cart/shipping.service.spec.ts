import type { Mock } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { CartService } from './cart.service';
import { ShippingService } from './shipping.service';

describe('ShippingService', () => {
  let service: ShippingService;
  let cartSpy: { getTotal: Mock<CartService['getTotal']> };

  beforeEach(() => {
    cartSpy = {
      getTotal: vi.fn<CartService['getTotal']>().mockName('CartService.getTotal'),
    };

    TestBed.configureTestingModule({
      providers: [{ provide: CartService, useValue: cartSpy }],
    });
    service = TestBed.inject(ShippingService);
  });

  it('should offer free shipping for orders over 500 TL', () => {
    cartSpy.getTotal.mockReturnValue(650);

    expect(service.getShippingFee()).toBe(0);
    expect(cartSpy.getTotal).toHaveBeenCalled();
  });

  it('should charge 40 TL below 500 TL', () => {
    cartSpy.getTotal.mockReturnValue(320);

    expect(service.getShippingFee()).toBe(40);
  });
});
