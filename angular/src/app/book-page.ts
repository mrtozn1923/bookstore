import { Component, input } from '@angular/core';

@Component({
  selector: 'app-book-page',
  template: `
    <!-- the same view-transition-name as the cover in the card: the browser moves one image to the other's place -->
    <img [src]="slug() + '.svg'" [alt]="'Cover of ' + slug()" width="240" height="360"
         [style.view-transition-name]="'cover-' + slug()">
    <h1>{{ slug() }}</h1>
  `,
  styles: `
    :host {
      display: block;
      padding: 1rem;
    }
  `,
})
export class BookPage {
  // filled from the :slug route parameter by withComponentInputBinding()
  readonly slug = input.required<string>();
}
