import { Component } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { NgOptimizedImage } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Book } from '../catalog/book';

@Component({
  selector: 'app-home-page',
  imports: [RouterLink, NgOptimizedImage],
  template: `
    @if (featured.value(); as book) {
      <section class="hero">
        <!-- A real <img>: discoverable, prioritizable; priority = fetchpriority="high" + no lazy loading -->
        <img class="hero-cover" [ngSrc]="book.coverUrl!" width="1200" height="1800" priority
             sizes="(max-width: 640px) 100vw, 360px" [alt]="book.title" />
        <div>
          <p class="eyebrow">Book of the week</p>
          <h1>{{ book.title }}</h1>
          <p class="author">{{ book.author }}</p>
          <p>{{ book.summary }}</p>
          <a [routerLink]="['/books', book.isbn]">View details</a>
        </div>
      </section>
    }
    <h2>New arrivals</h2>
    <ul class="grid">
      @for (book of newArrivals.value() ?? []; track book.isbn) {
        <li>
          <a [routerLink]="['/books', book.isbn]">
            <img [ngSrc]="book.coverUrl!" width="1200" height="1800"
                 sizes="(max-width: 640px) 50vw, 180px" [alt]="book.title" />
            <span>{{ book.title }}</span>
          </a>
        </li>
      }
    </ul>
  `,
  styles: `
    .hero { display: grid; grid-template-columns: minmax(0, 360px) 1fr; gap: 24px; align-items: center; }
    .hero-cover { width: 100%; height: auto; border-radius: 12px; }
    .eyebrow { text-transform: uppercase; font-size: 13px; color: #64748b; }
    .grid { list-style: none; padding: 0; display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 16px; }
    .grid img { width: 100%; height: auto; border-radius: 8px; display: block; }
    @media (max-width: 640px) { .hero { grid-template-columns: 1fr; } }
  `,
})
export class HomePage {
  protected readonly featured = httpResource<Book>(() => '/api/books/featured');
  protected readonly newArrivals = httpResource<Book[]>(() => '/api/books/new');
}
