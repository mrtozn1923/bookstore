import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: `
    <header class="top">
      <a routerLink="/" class="logo">Bookstore</a>
      <nav><a routerLink="/books">Catalog</a></nav>
    </header>
    <main><router-outlet /></main>
  `,
  styles: `
    .top { display: flex; gap: 24px; align-items: center; padding: 12px 16px; background: #0f172a; }
    .top a { color: #fff; text-decoration: none; }
    .logo { font-weight: 700; font-size: 20px; }
    main { max-width: 1100px; margin: 0 auto; padding: 16px; }
  `,
})
export class App {}
