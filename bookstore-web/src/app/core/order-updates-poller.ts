import { effect, inject, Service } from '@angular/core';
import { OrderBadge } from './order-badge';
import { TabLeader } from './tab-leader';

type OrderUpdatesMessage = { type: 'count'; unread: number } | { type: 'refresh' };

function isOrderUpdatesMessage(data: unknown): data is OrderUpdatesMessage {
  const m = data as OrderUpdatesMessage;
  return (
    (m?.type === 'count' && Number.isInteger(m.unread) && m.unread >= 0) || m?.type === 'refresh'
  );
}

// Only the leader tab polls the server; every tab shows the count it broadcasts.
@Service()
export class OrderUpdatesPoller {
  private readonly badge = inject(OrderBadge);
  private readonly leader = inject(TabLeader);
  private readonly channel = new BroadcastChannel('bookstore-order-updates');
  private readonly intervalMs = Number(new URLSearchParams(location.search).get('poll') ?? 30_000);

  constructor() {
    this.channel.onmessage = ({ data }) => {
      if (!isOrderUpdatesMessage(data)) return; // same origin, but still untrusted input
      if (data.type === 'count') void this.badge.show(data.unread);
      else if (this.leader.isLeader()) void this.poll('asked by another tab');
    };
    effect((onCleanup) => {
      if (!this.leader.isLeader()) return;
      void this.poll('became leader');
      const timer = setInterval(() => void this.poll('timer'), this.intervalMs);
      onCleanup(() => clearInterval(timer));
    });
    // a tab the user comes back to wants a fresh number, whichever tab is the leader
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') this.refreshNow();
    });
  }

  refreshNow() {
    if (this.leader.isLeader()) void this.poll('asked by this tab');
    else this.channel.postMessage({ type: 'refresh' } satisfies OrderUpdatesMessage);
  }

  private async poll(reason: string) {
    const unread = await this.badge.refresh();
    console.log(`leader: polled (${reason}), unread ${unread}`);
    if (unread !== null)
      this.channel.postMessage({ type: 'count', unread } satisfies OrderUpdatesMessage);
  }
}
