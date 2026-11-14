import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CartService } from '../cart.service';

@Component({
    selector: 'app-cart-notifier',
    template: `<p>{{ message }}</p>`,
})
export class CartNotifierComponent implements OnInit, OnDestroy {
  message = '';
  private destroy$ = new Subject<void>();

  constructor(private cartService: CartService) { }

  ngOnInit(): void {
    this.cartService.count$
      .pipe(takeUntil(this.destroy$))
      .subscribe(count => this.message = `${count} books in the cart`);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
