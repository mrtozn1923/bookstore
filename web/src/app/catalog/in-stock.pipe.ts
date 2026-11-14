import { Pipe, PipeTransform } from '@angular/core';
import { Book } from '../book';

@Pipe({ name: 'inStock' })
export class InStockPipe implements PipeTransform {
  transform(books: Book[]): Book[] {
    console.log('inStock ran');
    return books.filter(b => b.stock > 0);
  }
}
