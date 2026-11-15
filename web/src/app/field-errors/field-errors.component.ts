import { Component, input } from '@angular/core';
import { FieldTree } from '@angular/forms/signals';

@Component({
  selector: 'app-field-errors',
  template: `
    @if (field()().touched()) {
      @for (error of field()().errors(); track error.kind) {
        <p class="error">{{ error.message }}</p>
      }
    }
  `
})
export class FieldErrorsComponent {
  readonly field = input.required<FieldTree<unknown>>();
}
