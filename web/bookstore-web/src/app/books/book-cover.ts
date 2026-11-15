import { Component, LOCALE_ID, computed, inject, input } from '@angular/core';
import { NgOptimizedImage } from '@angular/common';
import { Book, coverAlt } from './book';

@Component({
  selector: 'app-book-cover',
  imports: [NgOptimizedImage],
  template: `<img [ngSrc]="book().coverStem" [alt]="alt()" [width]="width()" [height]="width() * 1.5" [priority]="priority()" />`,
})
export class BookCover {
  readonly book = input.required<Book>();
  readonly width = input(320);
  // ilk ekrandaki ana kapak: lazy değil, fetchpriority=high, SSR'da preload
  readonly priority = input(false);
  private readonly locale = inject(LOCALE_ID);
  protected readonly alt = computed(() => coverAlt(this.book(), this.locale));
}
