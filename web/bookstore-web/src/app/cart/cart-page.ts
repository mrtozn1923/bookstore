import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, Injector, afterNextRender, computed, inject, signal, viewChild } from '@angular/core';
import { FormField, email, form, required } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { CartLine, OrderView } from '../core/api-models';
import { PageHeading } from '../core/page-heading';
import { CartStore } from './cart-store';

interface SummaryItem {
  message: string;
  fieldId: string | null;
}

@Component({
  selector: 'app-cart-page',
  imports: [CurrencyPipe, RouterLink, PageHeading, FormField],
  templateUrl: './cart-page.html',
})
export class CartPage {
  protected readonly store = inject(CartStore);
  protected readonly placedOrder = signal<OrderView | null>(null);
  protected readonly serverError = signal('');
  protected readonly pendingRemoval = signal<CartLine | null>(null);
  // Signal Forms: the model is a signal, the rules and their messages live in the schema
  protected readonly customer = signal({ name: '', email: '' });
  protected readonly checkoutForm = form(this.customer, path => {
    required(path.name, { message: 'Enter your full name.' });
    required(path.email, { message: 'Enter your e-mail address.' });
    email(path.email, { message: 'Enter an e-mail address like name@example.com.' });
  });
  // Messages appear after the first submit, not while the user is still typing
  private readonly submitted = signal(false);
  protected readonly nameError = computed(() => this.submitted() ? this.firstError(this.checkoutForm.name().errors()) : '');
  protected readonly emailError = computed(() => this.submitted() ? this.firstError(this.checkoutForm.email().errors()) : '');

  // One list for the summary: field errors first, in the order of the form, then the error from the API
  protected readonly errorSummary = computed<SummaryItem[]>(() => [
    ...(this.nameError() ? [{ message: this.nameError(), fieldId: 'customer-name' }] : []),
    ...(this.emailError() ? [{ message: this.emailError(), fieldId: 'customer-email' }] : []),
    ...(this.serverError() ? [{ message: this.serverError(), fieldId: null }] : []),
  ]);

  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly heading = viewChild.required<ElementRef<HTMLElement>>('heading');
  private readonly removeDialog = viewChild.required<ElementRef<HTMLDialogElement>>('removeDialog');
  private readonly summary = viewChild<ElementRef<HTMLElement>>('summary');
  private readonly confirmation = viewChild<ElementRef<HTMLElement>>('confirmation');

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

  protected async checkout(event: Event): Promise<void> {
    event.preventDefault();
    this.serverError.set('');
    this.submitted.set(true);
    if (this.errorSummary().length) {
      this.focusAfterRender(this.summary);
      return;
    }
    try {
      const { name, email } = this.customer();
      this.placedOrder.set(await this.store.placeOrder(name, email));
      this.focusAfterRender(this.confirmation);
    } catch (error) {
      this.serverError.set(this.describe(error));
      this.focusAfterRender(this.summary);
    }
  }

  private firstError(errors: { message?: string }[]): string {
    return errors[0]?.message ?? '';
  }

  // With <base href="/">, href="#customer-name" would leave the cart; move focus to the field ourselves.
  protected focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    this.host.nativeElement.querySelector<HTMLElement>(`#${fieldId}`)?.focus();
  }

  // Focus after the next render, when the element exists and its text is complete
  private focusAfterRender(target: () => ElementRef<HTMLElement> | undefined): void {
    afterNextRender(() => target()?.nativeElement.focus(), { injector: this.injector });
  }

  // The API answers with ProblemDetails: 400 validation errors or 409 when stock is not enough.
  private describe(error: unknown): string {
    const problem = error instanceof HttpErrorResponse ? error.error : null;
    if (error instanceof HttpErrorResponse && error.status === 409) {
      // The API lists the books by ISBN; the user knows them by title
      const titles = this.store.cart()?.items.filter(line => problem?.detail?.includes(line.isbn)).map(line => line.title) ?? [];
      if (titles.length) {
        return `Not enough stock for ${titles.join(', ')}. Lower the quantity and try again.`;
      }
    }
    return problem?.detail ?? problem?.title ?? 'The order could not be placed. Try again in a few minutes.';
  }
}
