import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

interface OrderUpdates {
  unread: number;
}

@Service()
export class OrderBadge {
  private readonly http = inject(HttpClient);
  readonly unread = signal(0);

  async refresh() {
    try {
      const { unread } = await firstValueFrom(this.http.get<OrderUpdates>('/api/me/order-updates'));
      await this.show(unread);
    } catch {
      // offline: keep the last count
    }
  }

  async markRead() {
    await firstValueFrom(this.http.post<OrderUpdates>('/api/me/order-updates/read', null));
    await this.show(0);
  }

  private async show(count: number) {
    this.unread.set(count);
    if (!('setAppBadge' in navigator)) return; // Firefox, and Safari outside an installed web app
    try {
      if (count > 0) await navigator.setAppBadge(count);
      else await navigator.clearAppBadge();
    } catch (err) {
      console.log(`badge: ${(err as DOMException).name}`);
    }
  }
}
