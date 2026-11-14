import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { Book } from './book';
import { BookService } from './book.service';

export const bookResolver: ResolveFn<Book | undefined> = (route) =>
  inject(BookService).getBook(route.paramMap.get('isbn') ?? '');
