import { Component, input } from '@angular/core';
import { BookList } from './book-list';
import { UiBlock } from './ui-blocks';

// Component registry: modelin seçebileceği her component burada açıkça listelenir
@Component({
  selector: 'app-generative-block',
  imports: [BookList],
  template: `
    @switch (block().component) {
      @case ('book-list') {
        <app-book-list [heading]="block().heading" [books]="block().books" />
      }
    }
  `,
})
export class GenerativeBlock {
  readonly block = input.required<UiBlock>();
}
