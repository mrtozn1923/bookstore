import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppConfigService } from './app-config.service';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  template: `
    <header>
      <h1>Bookstore</h1>
      <p id="env">{{ config.environment }} · API: {{ config.apiBaseUrl }}</p>
      <p>Free shipping over {{ config.freeShippingThreshold }} TRY</p>
    </header>
    <router-outlet />
  `,
})
export class App {
  protected readonly config = inject(AppConfigService).value;
}
