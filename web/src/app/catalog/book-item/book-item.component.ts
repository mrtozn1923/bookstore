import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { Book } from '../../book';
import { TruncatePipe } from '../truncate.pipe';
import { RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';

@Component({
    selector: 'app-book-item',
    templateUrl: './book-item.component.html',
    styleUrls: ['./book-item.component.css'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterLink, TruncatePipe, CurrencyPipe]
})
export class BookItemComponent {
  @Input() book!: Book;
  @Output() addToCart = new EventEmitter<Book>();

  onAdd(): void {
    this.addToCart.emit(this.book);
  }
}
