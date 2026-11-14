import { Injectable } from '@angular/core';
import { CartService } from './cart.service';

@Injectable({ providedIn: 'root' })
export class ShippingService {
  constructor(private cart: CartService) {}

  getShippingFee(): number {
    return this.cart.getTotal() > 500 ? 0 : 40;
  }
}
