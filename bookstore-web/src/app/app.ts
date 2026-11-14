import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AppUpdates } from './core/app-updates';
import { BookstoreDatabase } from './core/bookstore-database';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: `
    <header>
      <strong>Bookstore</strong>
      <nav>
        <a routerLink="/">Catalog</a>
        <a routerLink="/reports/orders">Order report</a>
        <a routerLink="/admin/covers">Covers</a>
        <a routerLink="/orders">My orders</a>
        <a routerLink="/settings">Settings</a>
      </nav>
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
  `,
})
export class App {
  protected readonly database = inject(BookstoreDatabase);
  protected readonly updates = inject(AppUpdates);
}
