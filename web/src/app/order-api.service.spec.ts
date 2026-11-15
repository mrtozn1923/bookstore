import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OrderApiService } from './order-api.service';
import { emptyCheckout } from './checkout-form/checkout.model';

describe('OrderApiService', () => {
  it('posts the order to the API', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const service = TestBed.inject(OrderApiService);
    let id = 0;
    service.place(emptyCheckout()).subscribe((placed) => id = placed.id);
    TestBed.inject(HttpTestingController).expectOne('http://localhost:3000/orders').flush({ id: 1044 });
    expect(id).toBe(1044);
  });
});
