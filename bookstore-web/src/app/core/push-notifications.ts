import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom, map } from 'rxjs';
import { OrderBadge } from './order-badge';

@Service()
export class PushNotifications {
  private readonly swPush = inject(SwPush);
  private readonly http = inject(HttpClient);

  // no service worker (ng serve), or no Push API in this browser
  readonly supported = this.swPush.isEnabled && 'PushManager' in window;
  readonly permission = signal(
    typeof Notification === 'undefined' ? 'denied' : Notification.permission,
  );
  readonly subscribed = toSignal(this.swPush.subscription.pipe(map((s) => s !== null)), {
    initialValue: false,
  });

  constructor() {
    if (!this.swPush.isEnabled) return;
    const badge = inject(OrderBadge);
    const router = inject(Router);
    // a push arrived while a tab is open: the number in the header and on the icon changes
    this.swPush.messages.subscribe((message) => {
      console.log(`push message: ${JSON.stringify(message)}`);
      void badge.refresh();
    });
    // the service worker already focused or opened a window; the app decides what to show
    this.swPush.notificationClicks.subscribe(({ action, notification }) => {
      console.log(`notification click: "${notification.title}" action="${action}"`);
      void router.navigateByUrl('/orders');
    });
  }

  // call only from a click: the permission prompt needs a user gesture
  async enable(): Promise<string> {
    try {
      const { publicKey } = await firstValueFrom(
        this.http.get<{ publicKey: string }>('/api/push/public-key'),
      );
      const subscription = await this.swPush.requestSubscription({ serverPublicKey: publicKey });
      await firstValueFrom(this.http.post('/api/push/subscriptions', subscription));
      return 'subscribed';
    } catch (err) {
      return `failed: ${(err as Error).message}`; // permission denied, or the push service is unreachable
    } finally {
      this.permission.set(Notification.permission);
    }
  }

  async disable() {
    const subscription = await firstValueFrom(this.swPush.subscription);
    if (!subscription) return;
    await this.swPush.unsubscribe();
    await firstValueFrom(
      this.http.delete('/api/push/subscriptions', { params: { endpoint: subscription.endpoint } }),
    );
  }
}
