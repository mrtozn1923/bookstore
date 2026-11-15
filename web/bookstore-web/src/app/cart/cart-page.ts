import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, Injector, afterNextRender, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CartLine, OrderView } from '../core/api-models';
import { PageHeading } from '../core/page-heading';
import { CartStore } from './cart-store';

@Component({
  selector: 'app-cart-page',
  imports: [CurrencyPipe, RouterLink, PageHeading],
  templateUrl: './cart-page.html',
})
export class CartPage {
  protected readonly store = inject(CartStore);
  protected readonly placedOrder = signal<OrderView | null>(null);
  protected readonly errorMessage = signal('');
  protected readonly pendingRemoval = signal<CartLine | null>(null);

  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly heading = viewChild.required<ElementRef<HTMLElement>>('heading');
  private readonly removeDialog = viewChild.required<ElementRef<HTMLDialogElement>>('removeDialog');

  protected askToRemove(line: CartLine): void {
    this.pendingRemoval.set(line);
    this.removeDialog().nativeElement.showModal();
  }

  protected async onRemoveDialogClosed(): Promise<void> {
    const dialog = this.removeDialog().nativeElement;
    const line = this.pendingRemoval();
    this.pendingRemoval.set(null);
    if (dialog.returnValue !== 'remove' || !line) {
      return; // "Keep it" or Escape: the dialog puts focus back on the Remove button that opened it
    }
    dialog.returnValue = '';
    const index = this.store.cart()?.items.findIndex(l => l.isbn === line.isbn) ?? 0;
    await this.store.remove(line.isbn);
    this.focusAfterRemoval(index);
  }

  // The button that opened the dialog has been removed with its row. Without this, focus falls back to <body>.
  private focusAfterRemoval(index: number): void {
    afterNextRender(() => {
      const buttons = this.host.nativeElement.querySelectorAll<HTMLButtonElement>('tbody button');
      const next = buttons[Math.min(index, buttons.length - 1)];
      (next ?? this.heading().nativeElement).focus();
    }, { injector: this.injector });
  }

  protected async changeQuantity(isbn: string, quantity: number): Promise<void> {
    if (quantity >= 1 && quantity <= 99) {
      await this.store.setQuantity(isbn, quantity);
    }
  }

  protected async checkout(event: Event, name: string, email: string): Promise<void> {
    event.preventDefault();
    this.errorMessage.set('');
    try {
      this.placedOrder.set(await this.store.placeOrder(name, email));
    } catch (error) {
      // The API answers with ProblemDetails: 400 validation errors or 409 when stock is not enough.
      const problem = error instanceof HttpErrorResponse ? error.error : null;
      this.errorMessage.set(problem?.detail ?? problem?.title ?? 'The order could not be placed.');
    }
  }
}
