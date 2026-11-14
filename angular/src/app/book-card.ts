import { Component, input } from '@angular/core';

@Component({
  selector: 'app-book-card',
  template: `
    <article class="card">
      <img [src]="cover()" [alt]="'Cover of ' + bookTitle()" width="120" height="180">
      <h3>{{ bookTitle() }}</h3>
      <p class="author">{{ author() }}</p>
    </article>
  `,
  styles: `
    /* the host element is the container: every app-book-card measures its own slot */
    :host {
      display: block;
      container: book-card / inline-size;
    }

    .card {
      display: grid;
      gap: 0.25rem 1rem;
    }

    /* a container cannot query itself, so the layout lives on the inner .card */
    @container book-card (width >= 32rem) {
      .card {
        grid-template-columns: auto minmax(0, 1fr);
        grid-template-rows: auto 1fr;
      }

      img {
        grid-row: span 2;
      }
    }
  `,
})
export class BookCard {
  readonly bookTitle = input.required<string>();
  readonly author = input.required<string>();
  readonly cover = input.required<string>();
}
