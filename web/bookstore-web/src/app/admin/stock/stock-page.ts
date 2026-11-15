import { CdkCopyToClipboard } from '@angular/cdk/clipboard';
import { CdkListbox, CdkOption } from '@angular/cdk/listbox';
import { CdkFixedSizeVirtualScroll, CdkVirtualForOf, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { MatInput } from '@angular/material/input';
import { MatProgressBar } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { StockRow } from '../../core/api-models';

// 5,000 stock rows: only the rows in view (plus a buffer) exist in the DOM
@Component({
  selector: 'app-stock-page',
  imports: [
    CdkVirtualScrollViewport, CdkFixedSizeVirtualScroll, CdkVirtualForOf, CdkCopyToClipboard, CdkListbox, CdkOption,
    MatFormField, MatLabel, MatInput, MatIconButton, MatIcon, MatProgressBar,
  ],
  templateUrl: './stock-page.html',
  styleUrl: './stock-page.scss',
})
export class StockPage {
  private readonly snackBar = inject(MatSnackBar);

  protected readonly warehouses = ['', 'Istanbul', 'Ankara', 'Izmir'];
  protected readonly warehouse = signal('');
  protected readonly search = signal('');

  protected readonly stock = httpResource<StockRow[]>(
    () => ({ url: '/api/admin/stock', params: { warehouse: this.warehouse() } }),
    { defaultValue: [] },
  );

  // Ctrl+F cannot see rows that are not rendered, so the page has its own search
  protected readonly rows = computed(() => {
    const term = this.search().trim().toLowerCase();
    const rows = this.stock.value();
    return term ? rows.filter(r => r.title.toLowerCase().includes(term) || r.isbn.includes(term)) : rows;
  });

  protected readonly trackById = (_: number, row: StockRow) => row.id;

  protected warehouseChanged(value: readonly unknown[]): void {
    this.warehouse.set((value[0] as string | undefined) ?? '');
  }

  protected copied(ok: boolean, isbn: string): void {
    this.snackBar.open(ok ? `Copied ${isbn}` : 'Could not copy', undefined, { duration: 2000 });
  }
}
