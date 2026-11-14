import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

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
      </nav>
    </header>
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
export class App {}
