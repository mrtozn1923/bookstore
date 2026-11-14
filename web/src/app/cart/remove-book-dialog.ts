import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { Book } from '../catalog/book';

@Component({
  selector: 'app-remove-book-dialog',
  imports: [MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>Remove {{ book.title }}?</h2>
    <mat-dialog-content>The book will be removed from your cart.</mat-dialog-content>
    <mat-dialog-actions>
      <button matButton [mat-dialog-close]="false">Keep it</button>
      <button matButton="filled" [mat-dialog-close]="true">Remove</button>
    </mat-dialog-actions>
  `,
})
export class RemoveBookDialog {
  protected readonly book = inject<Book>(MAT_DIALOG_DATA);
}
