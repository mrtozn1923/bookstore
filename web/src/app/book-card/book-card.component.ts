import { Component } from '@angular/core';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-book-card',
  imports: [CurrencyPipe],
  template: `
    <h3>{{ title }}</h3>
    <p>{{ price | currency:'TRY' }}</p>
  `,
  styleUrl: './book-card.component.css'
})
export class BookCardComponent {
  title = 'Dune';
  price = 450;
}
