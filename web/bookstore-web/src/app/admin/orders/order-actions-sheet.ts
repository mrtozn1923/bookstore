import { Component, inject } from '@angular/core';
import { MAT_BOTTOM_SHEET_DATA, MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { MatActionList, MatListItem, MatListItemTitle } from '@angular/material/list';

export type OrderAction = 'cancel' | 'clear';

// Bulk actions for the selected orders, as a sheet from the bottom edge (thumb reach on phones)
@Component({
  selector: 'app-order-actions-sheet',
  imports: [MatActionList, MatListItem, MatListItemTitle],
  template: `
    <mat-action-list>
      <button mat-list-item type="button" (click)="choose('cancel')">
        <span matListItemTitle>Cancel {{ data.count }} {{ data.count === 1 ? 'order' : 'orders' }}</span>
      </button>
      <button mat-list-item type="button" (click)="choose('clear')">
        <span matListItemTitle>Clear selection</span>
      </button>
    </mat-action-list>
  `,
})
export class OrderActionsSheet {
  protected readonly data = inject<{ count: number }>(MAT_BOTTOM_SHEET_DATA);
  private readonly sheetRef = inject<MatBottomSheetRef<OrderActionsSheet, OrderAction>>(MatBottomSheetRef);

  protected choose(action: OrderAction): void {
    this.sheetRef.dismiss(action);
  }
}
