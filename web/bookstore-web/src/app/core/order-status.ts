import { effect, inject, Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { Session } from './session';

export interface OrderStatusChanged {
  orderId: string;
  status: string;
  reason: string | null;
}

// One SignalR connection per tab, opened once the user is signed in. The connection goes to the BFF
// (same origin, same cookie); the BFF forwards it to Notifications with the user's access token.
@Injectable({ providedIn: 'root' })
export class OrderStatusHub {
  readonly state = signal<'disconnected' | 'connecting' | 'connected' | 'reconnecting'>('disconnected');
  // The last push; pages compare orderId and reload the order from Ordering
  readonly lastChange = signal<OrderStatusChanged | undefined>(undefined);
  // Increases after every reconnect: pushes sent while the connection was down are lost, so pages re-read
  readonly resyncs = signal(0);

  private connection?: HubConnection;

  constructor() {
    const session = inject(Session);
    effect(() => {
      if (session.isSignedIn() && !this.connection) {
        void this.start();
      }
    });
  }

  private async start(): Promise<void> {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/orders')
      // Waits 0, 2, 10 and 30 seconds between attempts, then gives up (the client's default delays)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    this.connection = connection;

    connection.on('OrderStatusChanged', (change: OrderStatusChanged) => this.lastChange.set(change));
    connection.onreconnecting(() => this.state.set('reconnecting'));
    connection.onreconnected(() => {
      this.state.set('connected');
      this.resyncs.update(n => n + 1);
    });
    connection.onclose(() => this.state.set('disconnected'));

    this.state.set('connecting');
    try {
      await connection.start();
      this.state.set(connection.state === HubConnectionState.Connected ? 'connected' : 'disconnected');
    } catch {
      this.state.set('disconnected');
    }
  }
}
