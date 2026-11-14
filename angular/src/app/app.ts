import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  imports: [RouterLink, RouterOutlet],
  selector: 'app-root',
  template: `
    <header><a routerLink="/">Bookstore</a></header>
    <router-outlet />
  `,
})
export class App {}
