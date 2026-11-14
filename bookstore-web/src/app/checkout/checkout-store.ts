import { computed, DestroyRef, inject, Service, signal } from '@angular/core';
import { CartStore } from '../cart/cart-store';

export type ShippingMethod = 'standard' | 'express';
let instances = 0;

// Feature state: not provided in root; the 'checkout' route lists it in its providers
@Service({ autoProvided: false })
export class CheckoutStore {
  readonly #cart = inject(CartStore);
  readonly #instance = ++instances;
  readonly #shipping = signal<ShippingMethod>('standard');

  readonly shipping = this.#shipping.asReadonly();
  readonly shippingPrice = computed(() =>
    this.#shipping() === 'express' ? 90 : this.#cart.totalPrice() >= 500 ? 0 : 40,
  );
  readonly payable = computed(() => this.#cart.totalPrice() + this.shippingPrice());

  constructor() {
    console.log(`[checkout] store #${this.#instance} created`);
    inject(DestroyRef).onDestroy(() => console.log(`[checkout] store #${this.#instance} destroyed`));
  }

  chooseShipping(method: ShippingMethod): void {
    this.#shipping.set(method);
  }
}
