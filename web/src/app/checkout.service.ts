import { Injectable } from '@angular/core';
import { CartService } from './cart.service';

@Injectable({ providedIn: 'root' })
export class CheckoutService {

  constructor(private cartService: CartService) { }

  getShippingFee(): number {
    return this.cartService.getTotal() > 500 ? 0 : 40;
  }
}
