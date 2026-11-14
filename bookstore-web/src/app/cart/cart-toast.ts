import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { concat, map, of, switchMap, timer } from 'rxjs';
import { CartStore } from './cart-store';

@Component({
  selector: 'app-cart-toast',
  template: `
    @if (message(); as text) {
      <p class="toast" role="status">{{ text }}</p>
    }
  `,
})
export class CartToast {
  readonly message = toSignal(
    inject(CartStore).added$.pipe(
      // show the message for 3 seconds; a new event replaces the current one
      switchMap((book) =>
        concat(of(`${book.title} added to cart`), timer(3000).pipe(map(() => null))),
      ),
    ),
    { initialValue: null },
  );
}
