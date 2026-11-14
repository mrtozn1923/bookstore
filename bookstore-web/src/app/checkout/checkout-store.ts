import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withHooks, withMethods, withProps, withState } from '@ngrx/signals';
import { CartStore } from '../cart/cart-store';

export type ShippingMethod = 'standard' | 'express';
let instances = 0;

// Feature state: no providedIn; the 'checkout' route lists it in its providers
export const CheckoutStore = signalStore(
  withState({ shipping: 'standard' as ShippingMethod }),
  withProps(() => ({ _cart: inject(CartStore), _instance: ++instances })),
  withComputed(({ shipping, _cart }) => ({
    shippingPrice: computed(() => (shipping() === 'express' ? 90 : _cart.payable() >= 500 ? 0 : 40)),
  })),
  withComputed(({ shippingPrice, _cart }) => ({
    payable: computed(() => _cart.payable() + shippingPrice()),
  })),
  withMethods((store) => ({
    chooseShipping(shipping: ShippingMethod): void {
      patchState(store, { shipping });
    },
  })),
  withHooks({
    onInit: ({ _instance }) => console.log(`[checkout] store #${_instance} created`),
    onDestroy: ({ _instance }) => console.log(`[checkout] store #${_instance} destroyed`),
  }),
);
