import { Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { CartStore } from './cart/cart-store';
import { CartToast } from './cart/cart-toast';
import { ServerStatus } from './core/server-status';

@Component({
  imports: [RouterOutlet, RouterLink, CartToast],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly cart = inject(CartStore);
  protected readonly serverStatus = inject(ServerStatus);

  constructor() {
    // Router.events is an Observable: one line per finished navigation, e.g. for analytics
    inject(Router)
      .events.pipe(
        filter((event) => event instanceof NavigationEnd),
        map((event) => event.urlAfterRedirects),
        takeUntilDestroyed(),
      )
      .subscribe((url) => console.log(`[analytics] page view ${url}`));
  }
}
