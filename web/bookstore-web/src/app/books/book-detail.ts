import { Component, computed, inject, input } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { BOOKS } from './book';
import { BookCover } from './book-cover';

@Component({
  selector: 'app-book-detail',
  imports: [BookCover, RouterLink],
  template: `
    @if (book(); as b) {
      <h1>{{ b.title }}</h1>
      <p>{{ b.author }}, {{ b.year }}</p>
      <app-book-cover [book]="b" [priority]="true" />
      <h2>Related books</h2>
      <ul>
        @for (r of related(); track r.slug) {
          <li><a [routerLink]="['/books', r.slug]"><app-book-cover [book]="r" [width]="160" />{{ r.title }}</a></li>
        }
      </ul>
    }
  `,
})
export class BookDetail {
  readonly slug = input.required<string>();
  protected readonly book = computed(() => BOOKS.find((b) => b.slug === this.slug()));
  protected readonly related = computed(() => BOOKS.filter((b) => b.slug !== this.slug()).slice(0, 3));

  constructor() {
    inject(Title).setTitle('Bookstore');
  }
}
