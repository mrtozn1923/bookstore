import { Component, input } from '@angular/core';

@Component({
  selector: 'app-share-panel',
  template: `<p class="share">Share this book: https://bookstore.example.com/books/{{ isbn() }}</p>`,
})
export class SharePanel {
  // :isbn belongs to the parent route; the child gets it through params inheritance
  readonly isbn = input<string>();
}
