import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CartLine, CartService } from '../cart.service';

@Component({
  selector: 'app-cart',
  imports: [CurrencyPipe],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.css'
})
export class CartComponent {
  private readonly cart = inject(CartService);
  protected readonly lines = toSignal(this.cart.items$, { initialValue: [] });
  protected readonly total = computed(() => this.lines().reduce((sum, line) => sum + line.price, 0));

  protected remove(line: CartLine): void {
    this.cart.remove(line.lineId);
  }
}
