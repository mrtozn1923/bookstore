import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AppUpdates } from './core/app-updates';
import { BookstoreDatabase } from './core/bookstore-database';
import { InstallPrompt } from './core/install-prompt';
import { OrderBadge } from './core/order-badge';
import { PushNotifications } from './core/push-notifications';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: `
    <header>
      <!-- an installed app has no browser back button -->
      <button class="back" (click)="back()">Back</button>
      <strong>Bookstore</strong>
      <nav>
        <a routerLink="/">Catalog</a>
        <a routerLink="/search">Search</a>
        <a routerLink="/reports/orders">Order report</a>
        <a routerLink="/admin/covers">Covers</a>
        <a routerLink="/orders">
          My orders
          @if (badge.unread()) {
            ({{ badge.unread() }})
          }
        </a>
        <a routerLink="/settings">Settings</a>
      </nav>
      @if (installer.offer()) {
        <span class="install-offer">
          <button (click)="install()">Install app</button>
          <button (click)="installer.notNow()">Not now</button>
        </span>
      }
    </header>
    @if (database.outdated()) {
      <p class="banner">Bookstore was updated in another tab. Reload this page to continue.</p>
    }
    @if (updates.ready()) {
      <p class="banner update">
        A new version of Bookstore is ready.
        <button (click)="updates.reload()">Reload</button>
      </p>
    }
    @if (updates.broken()) {
      <p class="banner broken">
        Bookstore must reload to keep working.
        <button (click)="updates.reload()">Reload</button>
      </p>
    }
    <main>
      <router-outlet />
    </main>
  `,
  styles: `
    header {
      display: flex;
      gap: 1rem;
      align-items: center;
    }
    nav {
      display: flex;
      gap: 0.75rem;
    }
    .back {
      display: none;
    }
    @media (display-mode: standalone) {
      .back {
        display: inline;
      }
    }
  `,
})
export class App {
  protected readonly database = inject(BookstoreDatabase);
  protected readonly updates = inject(AppUpdates);
  protected readonly installer = inject(InstallPrompt);
  protected readonly badge = inject(OrderBadge);

  constructor() {
    void this.badge.refresh();
    inject(PushNotifications); // listens for push messages and notification clicks on every page
  }

  async install() {
    console.log(`install: ${await this.installer.install()}`);
  }

  back() {
    history.back();
  }
}
