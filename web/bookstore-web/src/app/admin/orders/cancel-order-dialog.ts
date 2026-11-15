import { Component, inject, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatOption } from '@angular/material/core';
import {
  MAT_DIALOG_DATA, MatDialogActions, MatDialogContent, MatDialogRef, MatDialogTitle,
} from '@angular/material/dialog';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatSelect } from '@angular/material/select';

// Only what the dialog shows: ids and a summary line, not the table's rows, services or callbacks
export interface CancelOrderData {
  orderIds: number[];
  summary: string;
}

export interface CancelOrderResult {
  reason: string;
}

@Component({
  selector: 'app-cancel-order-dialog',
  imports: [
    MatDialogTitle, MatDialogContent, MatDialogActions, MatButton, MatFormField, MatLabel, MatSelect, MatOption,
  ],
  template: `
    <h2 mat-dialog-title>Cancel {{ title }}?</h2>
    <mat-dialog-content>
      <p>{{ data.summary }}</p>
      <mat-form-field>
        <mat-label>Reason</mat-label>
        <mat-select [value]="reason()" (valueChange)="reason.set($event)">
          @for (r of reasons; track r) {
            <mat-option [value]="r">{{ r }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <!-- A bare mat-dialog-close closes with '' (the attribute's value), not undefined -->
      <button matButton (click)="dialogRef.close()">Keep order</button>
      <button matButton="filled" [disabled]="!reason()" (click)="confirm()">Cancel order</button>
    </mat-dialog-actions>
  `,
})
export class CancelOrderDialog {
  protected readonly data = inject<CancelOrderData>(MAT_DIALOG_DATA);
  protected readonly dialogRef = inject<MatDialogRef<CancelOrderDialog, CancelOrderResult>>(MatDialogRef);

  protected readonly title =
    this.data.orderIds.length === 1 ? `order #${this.data.orderIds[0]}` : `${this.data.orderIds.length} orders`;
  protected readonly reasons = ['Customer request', 'Out of stock', 'Payment failed', 'Duplicate order'];
  protected readonly reason = signal('');

  protected confirm(): void {
    this.dialogRef.close({ reason: this.reason() });
  }
}
