import {
  CdkDrag, CdkDragDrop, CdkDropList, CdkDropListGroup, moveItemInArray, transferArrayItem,
} from '@angular/cdk/drag-drop';
import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { firstValueFrom } from 'rxjs';
import { InstitutionalOrder, InstitutionalOrderLine } from '../../core/api-models';

type Slot = 'this' | 'next';

// Partial shipments of an institutional order: drag lines between the two lists or use the move buttons
@Component({
  selector: 'app-shipment-plan-page',
  imports: [CdkDropListGroup, CdkDropList, CdkDrag, MatButton],
  templateUrl: './shipment-plan-page.html',
  styleUrl: './shipment-plan-page.scss',
})
export class ShipmentPlanPage {
  private readonly http = inject(HttpClient);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly slots: { id: Slot; title: string; other: string; action: string }[] = [
    { id: 'this', title: 'This shipment', other: 'next', action: 'Later' },
    { id: 'next', title: 'Next shipment', other: 'this', action: 'Now' },
  ];
  protected readonly order = signal<InstitutionalOrder | null>(null);
  protected readonly lists = signal<Record<Slot, InstitutionalOrderLine[]>>({ this: [], next: [] });
  protected readonly saving = signal(false);
  protected readonly dirty = signal(false);
  protected readonly canSave = computed(() => this.dirty() && !this.saving() && this.lists().this.length > 0);

  constructor() {
    void firstValueFrom(this.http.get<InstitutionalOrder>('/api/admin/institutional-orders/1042')).then(order => {
      this.order.set(order);
      this.lists.set({
        this: order.lines.filter(l => l.shipment === 'This'),
        next: order.lines.filter(l => l.shipment === 'Next'),
      });
    });
  }

  protected copies(slot: Slot): number {
    return this.lists()[slot].reduce((sum, line) => sum + line.quantity, 0);
  }

  protected dropped(event: CdkDragDrop<Slot>): void {
    const from = event.previousContainer.data;
    const to = event.container.data;
    const next = { this: [...this.lists().this], next: [...this.lists().next] };
    if (from === to) {
      moveItemInArray(next[to], event.previousIndex, event.currentIndex);
    } else {
      transferArrayItem(next[from], next[to], event.previousIndex, event.currentIndex);
    }
    this.update(next);
  }

  // The same move without dragging: to the end of the other list
  protected moveTo(line: InstitutionalOrderLine, from: Slot): void {
    const to: Slot = from === 'this' ? 'next' : 'this';
    const next = { this: [...this.lists().this], next: [...this.lists().next] };
    transferArrayItem(next[from], next[to], next[from].indexOf(line), next[to].length);
    this.update(next);
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.put('/api/admin/institutional-orders/1042/shipments', {
        thisShipment: this.lists().this.map(l => l.id),
        nextShipment: this.lists().next.map(l => l.id),
      }));
      this.dirty.set(false);
      this.snackBar.open('Shipment plan saved', 'OK', { duration: 4000 });
    } catch {
      this.snackBar.open('The shipment plan could not be saved', 'OK', { duration: 6000 });
    } finally {
      this.saving.set(false);
    }
  }

  private update(next: Record<Slot, InstitutionalOrderLine[]>): void {
    this.lists.set(next);
    this.dirty.set(true);
  }
}
