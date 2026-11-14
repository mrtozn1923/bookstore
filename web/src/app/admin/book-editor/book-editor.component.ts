import { Component, input, output } from '@angular/core';
import { Book } from '../../book';

@Component({
  selector: 'app-book-editor',
  template: `
    <input #title [value]="book().title" />
    <button (click)="save(title.value)">Save</button>
    <button (click)="cancelled.emit()">Cancel</button>
  `
})
export class BookEditorComponent {
  book = input.required<Book>();
  saved = output<Book>();
  cancelled = output<void>();

  save(title: string): void {
    this.saved.emit({ ...this.book(), title });
  }
}
