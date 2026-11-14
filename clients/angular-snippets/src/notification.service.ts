import { Injectable, signal } from '@angular/core';

export interface Notification {
  title: string;
  detail?: string;
  traceId?: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly current = signal<Notification | null>(null);

  show(notification: Notification): void {
    this.current.set(notification);
  }
}
