import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { switchMap } from 'rxjs';
import { CreateOrder, OrderApi, OrderResult, ShipmentPreference } from './order-api';

// Order.MinimumQuantity invariant'ının (R2: en az 20 adet) istemcideki karşılığı
function minimumQuantity(control: AbstractControl): ValidationErrors | null {
  const quantity = control.value as number;
  return quantity < 20 ? { minimumQuantity: { required: 20, actual: quantity } } : null;
}

// R13: ilk sevkiyat alt sınırı yalnız kısmi sevkiyatta ve sipariş adedini aşamaz (Order constructor'ı)
function firstShipmentWithinQuantity(group: AbstractControl): ValidationErrors | null {
  const { quantity, shipmentPreference, minFirstShipment } = group.value as {
    quantity: number;
    shipmentPreference: ShipmentPreference;
    minFirstShipment: number | null;
  };
  if (minFirstShipment === null) return null;
  return shipmentPreference !== 'PartialAllowed' || minFirstShipment < 1 || minFirstShipment > quantity
    ? { firstShipment: true }
    : null;
}

@Component({
  selector: 'app-order-form',
  imports: [ReactiveFormsModule],
  template: `
    <form [formGroup]="form" (ngSubmit)="send()">
      <label>Book
        <select formControlName="bookId">
          <option [ngValue]="1">Dune</option>
          <option [ngValue]="2">Emma</option>
          <option [ngValue]="3">The Hobbit</option>
          <option [ngValue]="4">Middlemarch</option>
        </select>
      </label>
      <label>Quantity <input type="number" formControlName="quantity" /></label>
      <label>Budget category
        <select formControlName="budgetCategoryCode">
          <option value="library-books">Library Books</option>
          <option value="classroom-sets">Classroom Sets</option>
          <option value="teacher-resources">Teacher Resources</option>
          <option value="donation">Donation</option>
        </select>
      </label>
      <label>Delivery date <input type="date" formControlName="deliveryDate" /></label>
      <label>Shipment
        <select formControlName="shipmentPreference">
          <option value="SingleShipment">Single shipment</option>
          <option value="PartialAllowed">Partial shipment allowed</option>
        </select>
      </label>
      <label>Minimum first shipment <input type="number" formControlName="minFirstShipment" /></label>
      <label>Note <input formControlName="note" /></label>
      @if (form.controls.quantity.hasError('minimumQuantity')) {
        <p class="error">An institutional order needs at least 20 copies.</p>
      }
      @if (form.hasError('firstShipment')) {
        <p class="error">The first shipment needs partial shipment and cannot exceed the quantity.</p>
      }
      <button type="submit" [disabled]="form.invalid || busy()">Submit order</button>
    </form>
    @if (result(); as r) {
      <p class="result">Order #{{ r.id }} - {{ r.total }} TRY - status {{ r.status }}</p>
    }
    @if (error(); as e) {
      <p class="error">{{ e }}</p>
    }
  `,
})
export class OrderFormComponent {
  private readonly api = inject(OrderApi);
  protected readonly busy = signal(false);
  protected readonly result = signal<OrderResult | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      bookId: [4, Validators.required],
      quantity: [20, [Validators.required, minimumQuantity]],
      budgetCategoryCode: ['classroom-sets', Validators.required],
      deliveryDate: ['', Validators.required],
      shipmentPreference: ['SingleShipment' as ShipmentPreference],
      minFirstShipment: [null as number | null],
      note: [''],
    },
    { validators: firstShipmentWithinQuantity },
  );

  // Sequence diagram: create (Draft) -> submit (PendingApprover)
  protected send(): void {
    const v = this.form.getRawValue();
    const body: CreateOrder = { placedById: 4, ...v, note: v.note || null, submit: false };
    this.busy.set(true);
    this.error.set(null);
    this.api
      .create(body)
      .pipe(switchMap((draft) => this.api.submit(draft.id)))
      .subscribe({
        next: (r) => {
          this.result.set(r);
          this.busy.set(false);
        },
        error: (e: HttpErrorResponse) => {
          // ProblemDetails: 422 iş kuralı, 409 geçersiz geçiş
          this.error.set(e.error?.detail ?? e.message);
          this.busy.set(false);
        },
      });
  }
}
