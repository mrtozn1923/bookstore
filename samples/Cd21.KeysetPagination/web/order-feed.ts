import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';

export interface CursorPage<T> { items: T[]; next: string | null; previous: string | null; }
export interface OrderRow { id: number; createdAt: string; totalAmount: number; }

@Injectable({ providedIn: 'root' })
export class OrderFeed {
  private readonly http = inject(HttpClient);
  readonly rows = signal<OrderRow[]>([]);
  readonly loading = signal(false);
  readonly hasMore = signal(true);
  private next: string | null = null;

  // called when the list end comes into view (IntersectionObserver)
  loadMore(): void {
    if (this.loading() || !this.hasMore()) {
      return;
    }
    this.loading.set(true);
    const params: Record<string, string> = this.next ? { pageSize: '50', cursor: this.next } : { pageSize: '50' };
    this.http.get<CursorPage<OrderRow>>('/api/orders', { params }).subscribe({
      next: page => {
        this.rows.update(rows => [...rows, ...page.items]);
        this.next = page.next;
        this.hasMore.set(page.next !== null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  // a filter or sort change invalidates the cursor: start over
  reset(): void {
    this.rows.set([]);
    this.next = null;
    this.hasMore.set(true);
  }
}
