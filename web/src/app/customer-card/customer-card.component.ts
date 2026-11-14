import { Component } from '@angular/core';
import { UpperCasePipe, SlicePipe, TitleCasePipe } from '@angular/common';

@Component({
    selector: 'app-customer-card',
    template: `
    <p>{{ customer | titlecase }}</p>
    <p>{{ customer | uppercase }}</p>
    <p>{{ city | uppercase }}</p>
    <p>{{ city | titlecase }}</p>
    <p>{{ city | slice:0:3 | uppercase }}</p>
  `,
    standalone: true,
    imports: [UpperCasePipe, SlicePipe, TitleCasePipe]
})
export class CustomerCardComponent {
  customer = 'ada lovelace';
  city = 'istanbul';
}
