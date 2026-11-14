import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, Resolve } from '@angular/router';
import { Observable } from 'rxjs';
import { Book } from './book';
import { BookService } from './book.service';

@Injectable({ providedIn: 'root' })
export class BookResolver implements Resolve<Book | undefined> {

  constructor(private bookService: BookService) { }

  resolve(route: ActivatedRouteSnapshot): Observable<Book | undefined> {
    const isbn = route.paramMap.get('isbn') ?? '';
    return this.bookService.getBook(isbn);
  }
}
